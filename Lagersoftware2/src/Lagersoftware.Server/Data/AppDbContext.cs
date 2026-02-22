using Microsoft.EntityFrameworkCore;
using Lagersoftware.Shared.Models;

namespace Lagersoftware.Server.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<StorageLocation> StorageLocations => Set<StorageLocation>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<StockMovement> StockMovements => Set<StockMovement>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Product>()
            .HasOne(p => p.Category)
            .WithMany(c => c.Products)
            .HasForeignKey(p => p.CategoryId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<Product>()
            .HasOne(p => p.StorageLocation)
            .WithMany(s => s.Products)
            .HasForeignKey(p => p.StorageLocationId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<StockMovement>()
            .HasOne(m => m.Product)
            .WithMany(p => p.StockMovements)
            .HasForeignKey(m => m.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Product>()
            .Property(p => p.Price)
            .HasColumnType("TEXT");
    }
}
