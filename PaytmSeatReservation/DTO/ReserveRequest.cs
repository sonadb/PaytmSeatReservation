namespace PaytmSeatReservation.DTOs
{
    public class ReserveRequest
    {
        public List<string> Seats { get; set; } = new();
        public string IdempotencyKey { get; set; } = string.Empty;
    }
}