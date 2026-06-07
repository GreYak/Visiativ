namespace Visiativ.ApiService.Models;

// Miroir de BasketService.Models.BasketItem
public record BasketItem(Guid ProductId, string Name, decimal Price, int Quantity);
