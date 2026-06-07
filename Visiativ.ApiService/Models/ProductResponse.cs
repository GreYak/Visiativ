namespace Visiativ.ApiService.Models;

// Miroir de CatalogService.Infrastructure.Api.ProductResponse
public record ProductResponse(Guid Id, string Name, string Description, decimal Price, int Stock);
