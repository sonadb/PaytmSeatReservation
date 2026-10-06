using Microsoft.EntityFrameworkCore;
using PaytmSeatReservation.Models;

namespace PaytmSeatReservation.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        public DbSet<Show> Shows => Set<Show>();
        public DbSet<Seat> Seats => Set<Seat>();
        public DbSet<Reservation> Reservations => Set<Reservation>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Show -> Seats
            modelBuilder.Entity<Show>()
                .HasMany(s => s.Seats)
                .WithOne(s => s.Show)
                .HasForeignKey(s => s.ShowId)
                .OnDelete(DeleteBehavior.NoAction);

            // Show -> Reservations
            modelBuilder.Entity<Show>()
                .HasMany(s => s.Reservations)
                .WithOne(r => r.Show)
                .HasForeignKey(r => r.ShowId)
                .OnDelete(DeleteBehavior.Cascade);

            // One seat number must be unique within a show
            modelBuilder.Entity<Seat>()
                .HasIndex(s => new { s.ShowId, s.SeatNumber })
                .IsUnique();

            // Same idempotency key can be used only once for a show
            modelBuilder.Entity<Reservation>()
                .HasIndex(r => new { r.ShowId, r.IdempotencyKey })
                .IsUnique();

            // Reservation -> Seats
            modelBuilder.Entity<Reservation>()
                .HasMany(r => r.Seats)
                .WithOne(s => s.Reservation)
                .HasForeignKey(s => s.ReservationId)
                .OnDelete(DeleteBehavior.SetNull);
        }
    }
}