using Visiativ.ApiService.Abstractions;
using Visiativ.ApiService.Models;

namespace Visiativ.ApiService.Endpoints;

public static class CatalogEndpoints
{
    public static void MapCatalogEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/products").WithTags("Products");

        group.MapGet("/", async (ICatalogClient catalog, CancellationToken ct) =>
        {
            var products = await catalog.GetAllProductsAsync(ct);
            return Results.Ok(products.Select(ProductDto.From));
        })
        .WithName("BFF_GetAllProducts")
        .WithSummary("Retourne tous les produits du catalogue.")
        .Produces<IEnumerable<ProductDto>>();
    }
}
