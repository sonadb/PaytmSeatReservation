namespace PaytmSeatReservation.DTOs
{
    public class ReservationResponse
    {
        public Guid ReservationId { get; set; }
        public Guid ShowId { get; set; }
        public string UserId { get; set; } = string.Empty;
        public List<string> Seats { get; set; } = new();
        public int AmountPaise { get; set; }
        public string Status { get; set; } = string.Empty;
    }
}