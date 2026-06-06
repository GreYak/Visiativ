using CatalogService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CatalogService.Infrastructure.Api
{
    public static class ProductEndpoints
    {
        public static void MapProductEndpoints(this IEndpointRouteBuilder endpoints)
        {
            var group = endpoints.MapGroup("/products").WithTags("Products");

            group.MapGet("/", async (CatalogDbContext db, CancellationToken ct) =>
                        Results.Ok(await db.Products
                            .AsNoTracking()
                            .OrderBy(p => p.Name)
                            .Select(p => new ProductResponse(p.Id, p.Name, p.Description, p.Price, p.Stock))
                            .ToListAsync(ct)))
                        .WithName("GetProducts")
                        .WithSummary("Returns all available products.")
                        .Produces<IReadOnlyList<ProductResponse>>();

            group.MapGet("/{id:guid}", async (CatalogDbContext db, Guid id, CancellationToken ct) =>
            {
                var product = await db.Products
                    .AsNoTracking()
                    .Where(p => p.Id == id)
                    .Select(p => new ProductResponse(p.Id, p.Name, p.Description, p.Price, p.Stock))
                    .FirstOrDefaultAsync(ct);

                return product is null
                    ? Results.NotFound(new { Message = $"Product '{id}' not found." })
                    : Results.Ok(product);
            })
                .WithName("GetProductById")
                .WithSummary("Returns a single product by id.")
                .Produces<ProductResponse>()
                .Produces(StatusCodes.Status404NotFound);
        }
    }
}
