namespace PaytmSeatReservation.Models
{
    public class Seat
    {
        public Guid Id { get; set; }

        public Guid ShowId { get; set; }
        public Show Show { get; set; } = null!;

        public string SeatNumber { get; set; } = string.Empty;

        public string Status { get; set; } = "available";

        public Guid? ReservationId { get; set; }
        public Reservation? Reservation { get; set; }
    }
}