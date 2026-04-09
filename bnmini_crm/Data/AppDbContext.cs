using bnmini_crm.Models;
using Microsoft.EntityFrameworkCore;

namespace bnmini_crm.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Venue> Venues => Set<Venue>();
        public DbSet<AppUser> AppUsers => Set<AppUser>();
        public DbSet<VenueUser> VenueUsers => Set<VenueUser>();
        public DbSet<Item> Items => Set<Item>();
        public DbSet<Order> Orders => Set<Order>();
        public DbSet<OrderItem> OrderItems => Set<OrderItem>();
        public DbSet<DeliveryAddress> DeliveryAddresses => Set<DeliveryAddress>();
        public DbSet<Category> Categories => Set<Category>();
        public DbSet<ItemImage> ItemImages { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Уникальный пользователь в заведении
            modelBuilder.Entity<VenueUser>()
                .HasIndex(vu => new { vu.VenueId, vu.AppUserId })
                .IsUnique();

            // Один TelegramUserId — один AppUser
            modelBuilder.Entity<AppUser>()
                .HasIndex(u => u.TelegramUserId)
                .IsUnique();
        }
    }
}
