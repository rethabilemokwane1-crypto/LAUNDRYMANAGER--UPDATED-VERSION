using LaundryManager.Models;
using Microsoft.EntityFrameworkCore;

namespace LaundryManager.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

        public DbSet<Machine> Machines { get; set; }
        public DbSet<FaultReport> FaultReports { get; set; }
        public DbSet<Residence> Residences { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<Booking> Bookings { get; set; }
        public DbSet<PushSubscription> PushSubscriptions { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Machine>()
                .HasOne(m => m.Residence)
                .WithMany(r => r.WashingMachines)
                .HasForeignKey(m => m.ResidenceId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<User>()
                .HasOne(u => u.Residence)
                .WithMany(r => r.Users)
                .HasForeignKey(u => u.ResidenceId);

            modelBuilder.Entity<Booking>()
                .HasOne(b => b.Machine)
                .WithMany()
                .HasForeignKey(b => b.MachineId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Booking>()
                .HasIndex(b => new { b.MachineId, b.SlotStart, b.SlotEnd });
        }
    }
}
