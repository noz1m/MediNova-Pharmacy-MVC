using Domain.Entities;
using Microsoft.EntityFrameworkCore;
namespace Infrastructure.Data;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Manufacturer> Manufacturers => Set<Manufacturer>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<Category>(b => { b.ToTable("categories"); b.HasIndex(x => x.Name).IsUnique(); });
        modelBuilder.Entity<Manufacturer>(b => { b.ToTable("manufacturers"); b.HasIndex(x => x.Name).IsUnique(); });
        modelBuilder.Entity<Product>(b =>
        {
            b.ToTable("products"); b.Property(x => x.Price).HasPrecision(18, 2);
            b.HasIndex(x => x.Name); b.HasOne(x => x.Category).WithMany(x => x.Products).HasForeignKey(x => x.CategoryId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne(x => x.Manufacturer).WithMany(x => x.Products).HasForeignKey(x => x.ManufacturerId).OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<Order>(b => { b.ToTable("orders"); b.Property(x => x.Total).HasPrecision(18, 2); b.HasIndex(x => x.CreatedAtUtc); });
        modelBuilder.Entity<OrderItem>(b =>
        {
            b.ToTable("order_items"); b.Property(x => x.UnitPrice).HasPrecision(18, 2);
            b.HasOne(x => x.Order).WithMany(x => x.Items).HasForeignKey(x => x.OrderId).OnDelete(DeleteBehavior.Cascade);
            b.HasOne(x => x.Product).WithMany(x => x.OrderItems).HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.SetNull);
        });
    }
}
