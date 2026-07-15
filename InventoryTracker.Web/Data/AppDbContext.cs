using InventoryTracker.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace InventoryTracker.Web.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }
        public DbSet<Product> Products => Set<Product>();
        public DbSet<StockMovement> StockMovements => Set<StockMovement>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Product>(entity =>
            {
                entity.HasIndex(p => p.Sku).IsUnique();

                entity.HasMany(p => p.Movements)
                      .WithOne(m => m.Product!)
                      .HasForeignKey(m => m.ProductId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<StockMovement>(entity =>
            {
                entity.Property(m => m.Type).HasConversion<int>();
                entity.HasIndex(m => new { m.ProductId, m.CreatedUtc });
            });
        }
    }
}
