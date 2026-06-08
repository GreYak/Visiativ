namespace Visiativ.ApiService.Clients;

/// <summary>Corps de la requête POST /api/basket/add envoyée à BasketService.</summary>
public record AddBasketItemExt(Guid ProductId, int Quantity, int? LimitMax);
