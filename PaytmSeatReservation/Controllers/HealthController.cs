using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PaytmSeatReservation.Data;

namespace PaytmSeatReservation.Controllers
{
    [ApiController]
    [Route("health")]
    public class HealthController : ControllerBase
    {
        private readonly AppDbContext _db;

        public HealthController(AppDbContext db)
        {
            _db = db;
        }

        // GET /health/live
        [HttpGet("live")]
        public IActionResult Live()
        {
            return Ok(new
            {
                status = "healthy"
            });
        }

        // GET /health/ready
        [HttpGet("ready")]
        public async Task<IActionResult> Ready()
        {
            try
            {
                var databaseReady = await _db.Database.CanConnectAsync();

                if (!databaseReady)
                {
                    return StatusCode(503, new
                    {
                        status = "not_ready",
                        database = "unavailable"
                    });
                }

                return Ok(new
                {
                    status = "ready",
                    database = "available"
                });
            }
            catch
            {
                return StatusCode(503, new
                {
                    status = "not_ready",
                    database = "unavailable"
                });
            }
        }
    }
}