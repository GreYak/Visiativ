using Visiativ.ApiService.Abstractions;
using Visiativ.ApiService.Models;

namespace Visiativ.ApiService.Clients;

public class BasketClient(HttpClient http) : IBasketClient
{
    public async Task<IEnumerable<BasketItem>> GetBasketAsync(CancellationToken ct = default)
    {
        var result = await http.GetFromJsonAsync<IEnumerable<BasketItem>>("/api/basket", ct);
        return result ?? [];
    }

    public async Task AddItemAsync(BasketItem item, CancellationToken ct = default)
    {
        var response = await http.PostAsJsonAsync("/api/basket/add", item, ct);
        response.EnsureSuccessStatusCode();
    }

    public async Task ClearBasketAsync(CancellationToken ct = default)
    {
        var response = await http.DeleteAsync("/api/basket", ct);
        response.EnsureSuccessStatusCode();
    }
}
