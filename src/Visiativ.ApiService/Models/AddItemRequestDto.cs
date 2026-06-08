namespace Visiativ.ApiService.Models;

/// <summary>Corps de la requête POST /basket/items reçue du frontend.</summary>
public record AddItemRequestDto(Guid ProductId, int Quantity);
