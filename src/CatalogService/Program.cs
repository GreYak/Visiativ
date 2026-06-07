using CatalogService.Infrastructure.Api;
using CatalogService.Infrastructure.Persistence;
using CatalogService.Infrastructure.Persistence.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
    builder.AddServiceDefaults();

    var connectionString = builder.Configuration.GetConnectionString("catalogdb") ?? throw new InvalidOperationException("Missing connection string 'catalogdb'");
    builder.Services.AddDbContext<CatalogDbContext>(options =>options.UseSqlServer(connectionString));


var app = builder.Build();

    if (app.Environment.IsDevelopment())
    {
        try
        {
            app.Logger.LogInformation("Database migration starting...");
            using var sp = app.Services.CreateScope();
            var db = sp.ServiceProvider.GetRequiredService<CatalogDbContext>();
            await db.Database.MigrateAsync();
            await SeedData.InitializeAsync(db);
            app.Logger.LogInformation("Database migration completed.");
        }
        catch (Exception e)
        {
            app.Logger.LogCritical(e, "Database migration failed. Application startup aborted.");
            return;
        }
    }

    app.UseRequestLogging();
    app.UseExceptionHandlingMiddleware();
    app.MapDefaultEndpoints();
    app.MapProductEndpoints();
    app.Run();

// Requis pour WebApplicationFactory<Program> dans les tests
public partial class Program { }
