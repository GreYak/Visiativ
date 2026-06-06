using CatalogService.Infrastructure.Api;
using CatalogService.Infrastructure.Middleware;
using CatalogService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
    builder.AddServiceDefaults();

    var connectionString = builder.Configuration.GetConnectionString("catalogdb") ?? throw new InvalidOperationException("Missing connection string 'catalogdb'");
    builder.Services.AddDbContext<CatalogDbContext>(options =>options.UseSqlServer(connectionString));


var app = builder.Build();
    if (app.Environment.IsDevelopment())
    {
        // Database migration
        try
        {
            Console.WriteLine("Starting database migration...");
            using var sp = app.Services.CreateScope();
            sp.ServiceProvider.GetRequiredService<CatalogDbContext>()
                .Database.Migrate();

            Console.WriteLine("End database migration...");
        }
        catch (Exception e)
        {
            Console.WriteLine($"An error occurred while migrating the database: {e.Message}");
            return;
        }
    }
    app.UseMiddleware<ExceptionHandlingMiddleware>();
    app.MapDefaultEndpoints();      // ???????
    app.MapProductEndpoints();
    app.Run();

// Requis pour WebApplicationFactory<Program> dans les tests
public partial class Program { }
