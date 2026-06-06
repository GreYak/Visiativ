using CatalogService.Infrastructure.Api;
using CatalogService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
    builder.AddServiceDefaults();

    var connectionString = builder.Configuration.GetConnectionString("catalogdb") ?? throw new InvalidOperationException("Missing connection string 'catalogdb'");
    builder.Services.AddDbContext<CatalogDbContext>(options =>options.UseSqlServer(connectionString));


// Add services to the container.

var app = builder.Build();
    if (app.Environment.IsDevelopment())
    {
        try
        {
        app.Services.CreateScope()
            .ServiceProvider.GetRequiredService<CatalogDbContext>()
            .Database.Migrate();
        // using var sp = app.Services.CreateScope();
        //var logger = app.Services.GetRequiredService<ILogger<Program>>();
        //logger.LogInformation("Starting database migration...");

        //await sp.ServiceProvider.GetRequiredService<ExpenseDispatchDbContext>()
        //    .Database.MigrateAsync();

        //logger.LogInformation("End database migration...");

    }
        catch (Exception e)
        {
            //app.Services.GetRequiredService<ILogger<Program>>()
            //    .LogError(e, "An error occurred while migrating the database.");
            return;
        }   
    }
    app.MapDefaultEndpoints();      // ???????
    app.MapProductEndpoints();
    app.Run();


