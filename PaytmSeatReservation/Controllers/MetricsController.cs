using Microsoft.AspNetCore.Mvc;

namespace PaytmSeatReservation.Controllers
{
    [ApiController]
    [Route("metrics")]
    public class MetricsController : ControllerBase
    {
        // GET /metrics
        [HttpGet]
        public IActionResult GetMetrics()
        {
            return Ok(new
            {
                service = "PaytmSeatReservation",
                status = "running",
                timestamp = DateTime.UtcNow
            });
        }
    }
}