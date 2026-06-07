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
    c.BaseAddress = new Uri("http://basketservice"));

var app = builder.Build();

// Pipeline middleware — ordre important :
// 1. RequestLogging   : log toutes les requêtes/réponses avec niveau sémantique + elapsed time
// 2. ExceptionHandling: catch-all technique → JSON 500 uniforme
// Les erreurs métier (503, 400) sont traitées inline dans chaque endpoint.
app.UseRequestLogging();
app.UseExceptionHandlingMiddleware();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi(); // spec JSON sur /openapi/v1.json

    // Swagger UI (CDN) — aucun package supplémentaire requis
    app.MapGet("/swagger", () => Results.Content("""
        <!DOCTYPE html>
        <html>
        <head>
          <title>Visiativ API — Swagger UI</title>
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
}

app.MapCatalogEndpoints();
app.MapBasketEndpoints();
app.MapDefaultEndpoints();

app.Run();

// Requis pour WebApplicationFactory<Program> dans les tests
public partial class Program { }
