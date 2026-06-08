namespace Visiativ.ApiService.Clients;

/// <summary>Réponse de CatalogService — contrat externe, ne pas exposer au frontend.</summary>
public record ProductExt(Guid Id, string Name, string Description, decimal Price, int Stock);
