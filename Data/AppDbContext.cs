using EFCoreAdvanced.Entities;
using Microsoft.EntityFrameworkCore;

namespace EFCoreAdvanced.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Product> Products => Set<Product>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Product>(entity =>
        {
            
            entity.HasIndex(p => p.Name);

            entity.HasQueryFilter(p => !p.IsDeleted);

            entity.HasOne(p => p.Category)
                .WithMany(c => c.Products)
                .HasForeignKey(p => p.CategoryId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.Property(p => p.RowVersion)
                .IsRowVersion();
        });

        modelBuilder.Entity<Category>(entity =>
        {
            entity.HasKey(c => c.Id);
            entity.Property(c => c.Name).IsRequired().HasMaxLength(200);
        });

        SeedData(modelBuilder);
    }

    private static void SeedData(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Category>().HasData(
            new Category { Id = 1, Name = "Electronics" },
            new Category { Id = 2, Name = "Clothing" },
            new Category { Id = 3, Name = "Books" }
        );

        modelBuilder.Entity<Product>().HasData(
            new Product { Id = 1, Name = "Laptop", Price = 25000, CategoryId = 1, IsDeleted = false },
            new Product { Id = 2, Name = "Mouse", Price = 500, CategoryId = 1, IsDeleted = false },
            new Product { Id = 3, Name = "Keyboard", Price = 1500, CategoryId = 1, IsDeleted = false },
            new Product { Id = 4, Name = "T-Shirt", Price = 300, CategoryId = 2, IsDeleted = false },
            new Product { Id = 5, Name = "Jeans", Price = 1200, CategoryId = 2, IsDeleted = true }
        );
    }
}
