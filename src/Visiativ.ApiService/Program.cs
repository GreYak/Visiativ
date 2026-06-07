using Visiativ.ApiService.Abstractions;
using Visiativ.ApiService.Clients;
using Visiativ.ApiService.Endpoints;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.Services.AddOpenApi();

// Clients HTTP — URLs résolues par Aspire service discovery
builder.Services.AddHttpClient<ICatalogClient, CatalogClient>(c =>
    c.BaseAddress = new Uri("http://catalogservice"));

builder.Services.AddHttpClient<IBasketClient, BasketClient>(c =>
    c.BaseAddress = new Uri("http://basket-api"));

var app = builder.Build();

// Middleware catch-all partagé (log + JSON 500 uniforme).
// Positionné en premier : intercepte toute exception technique non gérée par les endpoints.
// Les erreurs métier (503 service indisponible, 400 validation) sont traitées inline
// dans chaque endpoint via try/catch explicite.
app.UseExceptionHandlingMiddleware();

if (app.Environment.IsDevelopment())
    app.MapOpenApi();

app.MapCatalogEndpoints();
app.MapBasketEndpoints();
app.MapDefaultEndpoints();

app.Run();

// Requis pour WebApplicationFactory<Program> dans les tests
public partial class Program { }
