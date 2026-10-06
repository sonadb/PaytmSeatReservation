using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PaytmSeatReservation.Data;
using PaytmSeatReservation.DTOs;
using PaytmSeatReservation.Models;

namespace PaytmSeatReservation.Controllers
{
    [ApiController]
    [Route("shows/{showId:guid}/reserve")]
    public class ReservationsController : ControllerBase
    {
        private readonly AppDbContext _db;

        public ReservationsController(AppDbContext db)
        {
            _db = db;
        }

        // POST /shows/{showId}/reserve
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status201Created)]
        public async Task<IActionResult> Reserve(
            Guid showId,
            [FromHeader(Name = "X-User-Id")] string userId,
            [FromBody] ReserveRequest request)
        {
            if (string.IsNullOrWhiteSpace(userId))
            {
                return Unauthorized(new
                {
                    error = "X-User-Id header is required"
                });
            }

            if (request.Seats == null || request.Seats.Count == 0)
            {
                return BadRequest(new
                {
                    error = "At least one seat is required"
                });
            }

            if (string.IsNullOrWhiteSpace(request.IdempotencyKey))
            {
                return BadRequest(new
                {
                    error = "Idempotency key is required"
                });
            }

            var requestedSeats = request.Seats
                .Select(x => x.Trim())
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct()
                .ToList();

            if (requestedSeats.Count != request.Seats.Count)
            {
                return BadRequest(new
                {
                    error = "Duplicate or empty seat numbers are not allowed"
                });
            }

            // IMPORTANT:
            // Serializable transaction protects the reservation decision
            // from concurrent requests.
            await using var transaction =
                await _db.Database.BeginTransactionAsync(
                    System.Data.IsolationLevel.Serializable);

            try
            {
                // Find show inside transaction
                var show = await _db.Shows
                    .FirstOrDefaultAsync(x => x.Id == showId);

                if (show == null)
                {
                    await transaction.RollbackAsync();

                    return NotFound(new
                    {
                        error = "Show not found"
                    });
                }

                // Create deterministic request hash
                var normalizedRequest = new
                {
                    Seats = requestedSeats
                        .OrderBy(x => x)
                        .ToList()
                };

                var requestJson = JsonSerializer.Serialize(normalizedRequest);
                var requestHash = ComputeHash(requestJson);

                // Check idempotency key inside transaction
                var existingReservation = await _db.Reservations
                    .Include(x => x.Seats)
                    .FirstOrDefaultAsync(x =>
                        x.ShowId == showId &&
                        x.IdempotencyKey == request.IdempotencyKey);

                if (existingReservation != null)
                {
                    if (existingReservation.UserId != userId)
                    {
                        await transaction.RollbackAsync();

                        return Conflict(new
                        {
                            error = "Idempotency key already belongs to another user"
                        });
                    }

                    if (existingReservation.RequestHash != requestHash)
                    {
                        await transaction.RollbackAsync();

                        return Conflict(new
                        {
                            error = "Same idempotency key was used with a different request"
                        });
                    }

                    await transaction.CommitAsync();

                    return StatusCode(
                        StatusCodes.Status201Created,
                        BuildResponse(existingReservation));
                }

                // Count current confirmed seats for this user.
                // Serializable transaction prevents another concurrent
                // request from changing this decision at the same time.
                var userReservationCount = await _db.Seats
                    .Where(x =>
                        x.ShowId == showId &&
                        x.Reservation != null &&
                        x.Reservation.UserId == userId &&
                        x.Reservation.Status == "confirmed")
                    .CountAsync();

                if (userReservationCount + requestedSeats.Count >
                    show.PerUserLimit)
                {
                    await transaction.RollbackAsync();

                    return Conflict(new
                    {
                        error = $"Per-user seat limit is {show.PerUserLimit}"
                    });
                }

                // Find requested seats.
                // With Serializable isolation, concurrent conflicting
                // reservation requests are serialized by SQL Server.
                var seats = await _db.Seats
                    .Where(x =>
                        x.ShowId == showId &&
                        requestedSeats.Contains(x.SeatNumber))
                    .ToListAsync();

                // Check missing seats
                if (seats.Count != requestedSeats.Count)
                {
                    var foundSeatNumbers = seats
                        .Select(x => x.SeatNumber)
                        .ToHashSet();

                    var missingSeats = requestedSeats
                        .Where(x => !foundSeatNumbers.Contains(x))
                        .ToList();

                    await transaction.RollbackAsync();

                    return Conflict(new
                    {
                        error = "One or more seats do not exist",
                        seats = missingSeats
                    });
                }

                // All-or-nothing reservation
                var unavailableSeats = seats
                    .Where(x => x.Status != "available")
                    .Select(x => new
                    {
                        seat = x.SeatNumber,
                        status = x.Status
                    })
                    .ToList();

                if (unavailableSeats.Count > 0)
                {
                    await transaction.RollbackAsync();

                    return Conflict(new
                    {
                        error = "One or more seats are not available",
                        seats = unavailableSeats
                    });
                }

                // Create reservation
                var reservation = new Reservation
                {
                    Id = Guid.NewGuid(),
                    ShowId = showId,
                    UserId = userId,
                    IdempotencyKey = request.IdempotencyKey,
                    RequestHash = requestHash,
                    AmountPaise = show.PricePaise * requestedSeats.Count,
                    Status = "confirmed",
                    CreatedAt = DateTime.UtcNow
                };

                // Mark seats as confirmed
                foreach (var seat in seats)
                {
                    seat.Status = "confirmed";
                    seat.Reservation = reservation;
                }

                _db.Reservations.Add(reservation);

                await _db.SaveChangesAsync();

                await transaction.CommitAsync();

                return StatusCode(
                    StatusCodes.Status201Created,
                    BuildResponse(reservation));
            }
            catch (DbUpdateException)
            {
                await transaction.RollbackAsync();

                return Conflict(new
                {
                    error = "Reservation conflict. Please retry with a new idempotency key."
                });
            }
            catch
            {
                await transaction.RollbackAsync();

                return StatusCode(
                    StatusCodes.Status500InternalServerError,
                    new
                    {
                        error = "Unexpected reservation error"
                    });
            }
        }

        // DELETE /shows/{showId}/reserve/{reservationId}
        [HttpDelete("{reservationId:guid}")]
        public async Task<IActionResult> CancelReservation(
            Guid showId,
            Guid reservationId,
            [FromHeader(Name = "X-User-Id")] string userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
            {
                return Unauthorized(new
                {
                    error = "X-User-Id header is required"
                });
            }

            await using var transaction =
                await _db.Database.BeginTransactionAsync(
                    System.Data.IsolationLevel.Serializable);

            try
            {
                var reservation = await _db.Reservations
                    .Include(x => x.Seats)
                    .FirstOrDefaultAsync(x =>
                        x.Id == reservationId &&
                        x.ShowId == showId);

                if (reservation == null)
                {
                    await transaction.RollbackAsync();

                    return NotFound(new
                    {
                        error = "Reservation not found"
                    });
                }

                if (reservation.UserId != userId)
                {
                    await transaction.RollbackAsync();
                    return Forbid();
                }

                if (reservation.Status == "cancelled")
                {
                    await transaction.RollbackAsync();

                    return Conflict(new
                    {
                        error = "Reservation is already cancelled"
                    });
                }

                reservation.Status = "cancelled";

                foreach (var seat in reservation.Seats)
                {
                    seat.Status = "available";
                    seat.ReservationId = null;
                    seat.Reservation = null;
                }

                await _db.SaveChangesAsync();

                await transaction.CommitAsync();

                return Ok(new
                {
                    reservationId = reservation.Id,
                    status = reservation.Status,
                    message = "Reservation cancelled successfully"
                });
            }
            catch
            {
                await transaction.RollbackAsync();

                return StatusCode(
                    StatusCodes.Status500InternalServerError,
                    new
                    {
                        error = "Unexpected cancellation error"
                    });
            }
        }

        private static ReservationResponse BuildResponse(
            Reservation reservation)
        {
            return new ReservationResponse
            {
                ReservationId = reservation.Id,
                ShowId = reservation.ShowId,
                UserId = reservation.UserId,
                Seats = reservation.Seats
                    .Select(x => x.SeatNumber)
                    .ToList(),
                AmountPaise = reservation.AmountPaise,
                Status = reservation.Status
            };
        }

        private static string ComputeHash(string input)
        {
            using var sha256 = SHA256.Create();

            var bytes = Encoding.UTF8.GetBytes(input);
            var hash = sha256.ComputeHash(bytes);

            return Convert.ToHexString(hash);
        }
    }
}