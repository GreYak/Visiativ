using CatalogService.Infrastructure.Api;
using CatalogService.Infrastructure.Persistence;
using CatalogService.Infrastructure.Persistence.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
    builder.AddServiceDefaults();
    builder.Services.AddOpenApi();

    var connectionString = builder.Configuration.GetConnectionString("catalogdb") ?? throw new InvalidOperationException("Missing connection string 'catalogdb'");
    builder.Services.AddDbContext<CatalogDbContext>(options =>options.UseSqlServer(connectionString));


var app = builder.Build();

    if (app.Environment.IsDevelopment())
    {
        app.MapOpenApi();

        app.MapGet("/swagger", () => Results.Content("""
            <!DOCTYPE html>
            <html>
            <head>
              <title>CatalogService — Swagger UI</title>
              <meta charset="utf-8"/>
              <meta name="viewport" content="width=device-width, initial-scale=1">
              <link rel="stylesheet" href="https://unpkg.com/swagger-ui-dist/swagger-ui.css"/>
            </head>
            <body>
              <div id="swagger-ui"></div>
              <script src="https://unpkg.com/swagger-ui-dist/swagger-ui-bundle.js"></script>
              <script>
                SwaggerUIBundle({
                  url: '/openapi/v1.json',
                  dom_id: '#swagger-ui',
                  presets: [SwaggerUIBundle.presets.apis, SwaggerUIBundle.SwaggerUIStandalonePreset],
                  layout: 'BaseLayout'
                });
              </script>
            </body>
            </html>
            """, "text/html"))
            .ExcludeFromDescription();

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
