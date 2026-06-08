using Visiativ.ApiService.Clients;

namespace Visiativ.ApiService.Models;

/// <summary>Produit exposé par le BFF au frontend.</summary>
public record ProductDto(Guid Id, string Name, string Description, decimal Price, int Stock)
{
    public static ProductDto From(ProductExt ext)
        => new(ext.Id, ext.Name, ext.Description, ext.Price, ext.Stock);
}
