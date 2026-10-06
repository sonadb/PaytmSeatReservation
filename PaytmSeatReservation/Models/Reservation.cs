namespace PaytmSeatReservation.Models
{
    public class Reservation
    {
        public Guid Id { get; set; }

        public Guid ShowId { get; set; }
        public Show Show { get; set; } = null!;

        public string UserId { get; set; } = string.Empty;

        public string IdempotencyKey { get; set; } = string.Empty;

        public string RequestHash { get; set; } = string.Empty;

        public int AmountPaise { get; set; }

        public string Status { get; set; } = "confirmed";

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<Seat> Seats { get; set; } = new List<Seat>();
    }
}