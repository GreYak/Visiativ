using Visiativ.ApiService.Clients;

namespace Visiativ.ApiService.Models;

/// <summary>
/// Vue consolidée d'un item panier, construite par le BFF
/// à partir d'un <see cref="BasketItemExt"/> (quantité) et d'un <see cref="ProductExt"/> (infos catalogue).
/// </summary>
public record BasketItemDto(
    Guid    ProductId,
    string  Name,
    string  Description,
    decimal Price,
    int     Quantity,
    int     Stock)
{
    public static BasketItemDto From(BasketItemExt item, ProductExt product)
        => new(item.ProductId, product.Name, product.Description, product.Price, item.Quantity, product.Stock);
}
