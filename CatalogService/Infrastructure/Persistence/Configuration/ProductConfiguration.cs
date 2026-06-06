using CatalogService.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CatalogService.Infrastructure.Persistence.Configuration
{
    internal class ProductConfiguration : IEntityTypeConfiguration<Product>
    {
        public void Configure(EntityTypeBuilder<Product> builder)
        {
            builder.HasKey(p => p.Id);

            builder.Property(p => p.Name)
                .IsRequired()
                .HasMaxLength(200);

            builder.Property(p => p.Description)
                .HasMaxLength(1000);

            builder.Property(p => p.Price)
                .HasColumnType("decimal(18,2)")
                .IsRequired();

            builder.Property(p => p.Stock)
                .IsRequired();

            //// Seed data — objets anonymes pour contourner les setters privés
            //builder.HasData(
            //    new { Id = Guid.Parse("22222222-0000-0000-0000-000000000001"), Name = "Laptop Pro 15", Description = "High-performance laptop with 16GB RAM and 512GB SSD", Price = 1299.99m, Stock = 25 },
            //    new { Id = Guid.Parse("22222222-0000-0000-0000-000000000002"), Name = "Wireless Mouse", Description = "Ergonomic wireless mouse, 12-month battery life", Price = 29.99m, Stock = 150 },
            //    new { Id = Guid.Parse("22222222-0000-0000-0000-000000000003"), Name = "Mechanical Keyboard", Description = "TKL mechanical keyboard, Cherry MX switches", Price = 89.99m, Stock = 75 },
            //    new { Id = Guid.Parse("22222222-0000-0000-0000-000000000004"), Name = "USB-C Hub 7-in-1", Description = "7-port USB-C hub with HDMI 4K and 100W PD", Price = 49.99m, Stock = 200 },
            //    new { Id = Guid.Parse("22222222-0000-0000-0000-000000000005"), Name = "27\" 4K Monitor", Description = "IPS 4K monitor, 144Hz, HDR400", Price = 599.99m, Stock = 12 }
            //);
        }
    }
}
