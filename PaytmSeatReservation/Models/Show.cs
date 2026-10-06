namespace PaytmSeatReservation.Models
{
    public class Show
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int PricePaise { get; set; }
        public int PerUserLimit { get; set; } = 4;

        public ICollection<Seat> Seats { get; set; } = new List<Seat>();
        public ICollection<Reservation> Reservations { get; set; } = new List<Reservation>();
    }
}