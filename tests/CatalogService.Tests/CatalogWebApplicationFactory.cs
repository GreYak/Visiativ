using CatalogService.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CatalogService.Tests;

public class CatalogWebApplicationFactory : WebApplicationFactory<Program>
{
    public string DatabaseName { get; } = Guid.NewGuid().ToString();

    private readonly Action<IServiceCollection>? _overrideDbContext;

    public CatalogWebApplicationFactory(Action<IServiceCollection>? overrideDbContext = null)
        => _overrideDbContext = overrideDbContext;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:catalogdb", "Server=fake");

        builder.ConfigureTestServices(services =>
        {
            // DbContext InMemory
            var toRemove = services
                .Where(d => d.ServiceType == typeof(DbContextOptions<CatalogDbContext>)
                         || d.ServiceType == typeof(CatalogDbContext)
                         || (d.ServiceType.IsGenericType && d.ServiceType.GetGenericArguments().Contains(typeof(CatalogDbContext))))   // IDbContextOptionsConfiguration<CatalogDbContext>
                .ToList();
            toRemove.ForEach(d => services.Remove(d));

            if (_overrideDbContext is not null)
                _overrideDbContext(services);
            else
                services.AddDbContext<CatalogDbContext>(o =>
                    o.UseInMemoryDatabase(DatabaseName));
        });
    }

    public async Task SeedAsync(Action<CatalogDbContext> seed)
    {
        await using var scope = Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
        seed(db);
        await db.SaveChangesAsync();
    }
}
