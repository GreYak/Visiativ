using Visiativ.ApiService.Abstractions;
using Visiativ.ApiService.Clients;
using Visiativ.ApiService.Endpoints;
using Visiativ.ApiService.ExceptionHandlers;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ServiceUnavailableExceptionHandler>();
builder.Services.AddExceptionHandler<RemoteValidationExceptionHandler>();
builder.Services.AddOpenApi();

// Clients HTTP — URLs résolues par Aspire service discovery
// Les noms correspondent aux ressources déclarées dans l'AppHost
builder.Services.AddHttpClient<ICatalogClient, CatalogClient>(c =>
    c.BaseAddress = new Uri("http://catalogservice"));

builder.Services.AddHttpClient<IBasketClient, BasketClient>(c =>
    c.BaseAddress = new Uri("http://basket-api"));

var app = builder.Build();

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
    app.MapOpenApi();

app.MapCatalogEndpoints();
app.MapBasketEndpoints();
app.MapDefaultEndpoints();

app.Run();

// Requis pour WebApplicationFactory<Program> dans les tests
public partial class Program { }
