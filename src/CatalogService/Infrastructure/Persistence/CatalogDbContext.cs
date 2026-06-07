using CatalogService.Domain;
using Microsoft.EntityFrameworkCore;
using CatalogService.Infrastructure.Persistence.Configuration;


namespace CatalogService.Infrastructure.Persistence
{
    public class CatalogDbContext(DbContextOptions<CatalogDbContext> options) : DbContext(options)
    {
        public DbSet<Product> Products => Set<Product>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
            => modelBuilder.ApplyConfiguration(new ProductConfiguration());
    }
}
