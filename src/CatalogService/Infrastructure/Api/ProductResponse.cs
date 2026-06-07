namespace CatalogService.Infrastructure.Api
{
    public record ProductResponse(
        Guid Id,
        string Name,
        string Description,
        decimal Price,
        int Stock
    );
}
