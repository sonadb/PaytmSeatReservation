using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PaytmSeatReservation.Data;
using PaytmSeatReservation.DTOs;
using PaytmSeatReservation.Models;

namespace PaytmSeatReservation.Controllers
{
    [ApiController]
    [Route("shows")]
    public class ShowsController : ControllerBase
    {
        private readonly AppDbContext _db;

        public ShowsController(AppDbContext db)
        {
            _db = db;
        }

        // POST /shows
        [HttpPost]
        public async Task<IActionResult> CreateShow(
            [FromBody] CreateShowRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Name))
            {
                return BadRequest(new
                {
                    error = "Show name is required"
                });
            }

            if (request.Seats == null || request.Seats.Count == 0)
            {
                return BadRequest(new
                {
                    error = "At least one seat is required"
                });
            }

            if (request.PricePaise < 0)
            {
                return BadRequest(new
                {
                    error = "Price cannot be negative"
                });
            }

            if (request.PerUserLimit <= 0)
            {
                return BadRequest(new
                {
                    error = "Per user limit must be greater than 0"
                });
            }

            var duplicateSeats = request.Seats
                .GroupBy(x => x)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key)
                .ToList();

            if (duplicateSeats.Count > 0)
            {
                return BadRequest(new
                {
                    error = "Duplicate seat numbers are not allowed",
                    seats = duplicateSeats
                });
            }

            var show = new Show
            {
                Id = Guid.NewGuid(),
                Name = request.Name,
                PricePaise = request.PricePaise,
                PerUserLimit = request.PerUserLimit
            };

            foreach (var seatNumber in request.Seats)
            {
                show.Seats.Add(new Seat
                {
                    Id = Guid.NewGuid(),
                    SeatNumber = seatNumber,
                    Status = "available"
                });
            }

            _db.Shows.Add(show);

            await _db.SaveChangesAsync();

            return Created(
                $"/shows/{show.Id}",
                new
                {
                    id = show.Id,
                    name = show.Name,
                    price_paise = show.PricePaise,
                    per_user_limit = show.PerUserLimit,
                    total_seats = show.Seats.Count
                });
        }

        // GET /shows/{id}
        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetShow(Guid id)
        {
            var show = await _db.Shows
                .Include(x => x.Seats)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (show == null)
            {
                return NotFound(new
                {
                    error = "Show not found"
                });
            }

            var available = show.Seats.Count(x => x.Status == "available");
            var held = show.Seats.Count(x => x.Status == "held");
            var confirmed = show.Seats.Count(x => x.Status == "confirmed");

            return Ok(new
            {
                id = show.Id,
                name = show.Name,
                price_paise = show.PricePaise,
                per_user_limit = show.PerUserLimit,

                total = show.Seats.Count,
                available,
                held,
                confirmed,

                seats = show.Seats.Select(x => new
                {
                    seat = x.SeatNumber,
                    status = x.Status
                })
            });
        }
    }
}