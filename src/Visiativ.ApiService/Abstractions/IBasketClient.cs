using Visiativ.ApiService.Clients;

namespace Visiativ.ApiService.Abstractions;

public interface IBasketClient
{
    Task<IEnumerable<BasketItemExt>> GetBasketAsync(CancellationToken ct = default);
    Task AddItemAsync(BasketItemExt item, int? limitMax = null, CancellationToken ct = default);
    Task ClearBasketAsync(CancellationToken ct = default);
}
