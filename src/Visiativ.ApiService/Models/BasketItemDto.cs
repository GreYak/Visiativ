namespace Visiativ.ApiService.Models;

/// <summary>
/// Vue consolidée d'un item panier, construite par le BFF
/// à partir d'un <see cref="BasketItem"/> (quantité) et d'un <see cref="ProductResponse"/> (infos catalogue).
/// </summary>
public record BasketItemDto(
    Guid    ProductId,
    string  Name,
    string  Description,
    decimal Price,
    int     Quantity,
    int     Stock);
