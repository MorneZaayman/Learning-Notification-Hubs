using LearningNotificationHubs.API.Models;
using Microsoft.EntityFrameworkCore;

namespace LearningNotificationHubs.API.Data
{
    public class ApplicationDbContext : DbContext
    {
        public DbSet<User> Users { get; set; }
        public DbSet<Device> Devices { get; set; }

        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {}

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<User>(user =>
            {
                user
                .HasKey(u => u.Username);
            });

            builder.Entity<Device>(device =>
            {
                device
                .HasKey(d => new { d.Id, d.Username });

                device
                .HasOne(d => d.User)
                .WithMany(u => u.Devices)
                .HasForeignKey(d => d.Username)
                .OnDelete(DeleteBehavior.Restrict);

                device
                .Property(d => d.Platform)
                .IsRequired();

                device
                .Property(d => d.PnsToken)
                .IsRequired();

                device
                .Property(d => d.RegistrationId)
                .IsRequired();

                
            });
        }
    }
}
