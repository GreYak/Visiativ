namespace Visiativ.ApiService.Clients;

/// <summary>Réponse de BasketService — contrat externe, ne pas exposer au frontend.</summary>
public record BasketItemExt(Guid ProductId, int Quantity);
