using Visiativ.ApiService.Models;

namespace Visiativ.ApiService.Abstractions;

public interface IBasketClient
{
    Task<IEnumerable<BasketItem>> GetBasketAsync(CancellationToken ct = default);
    Task AddItemAsync(BasketItem item, int? limitMax = null, CancellationToken ct = default);
    Task ClearBasketAsync(CancellationToken ct = default);
}
