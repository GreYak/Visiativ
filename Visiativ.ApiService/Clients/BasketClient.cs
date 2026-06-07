using Visiativ.ApiService.Abstractions;
using Visiativ.ApiService.Exceptions;
using Visiativ.ApiService.Models;

namespace Visiativ.ApiService.Clients;

public class BasketClient(HttpClient http) : IBasketClient
{
    public async Task<IEnumerable<BasketItem>> GetBasketAsync(CancellationToken ct = default)
    {
        try
        {
            var result = await http.GetFromJsonAsync<IEnumerable<BasketItem>>("/api/basket", ct);
            return result ?? [];
        }
        catch (HttpRequestException)
        {
            throw new ServiceUnavailableException("BasketService");
        }
    }

    public async Task AddItemAsync(BasketItem item, CancellationToken ct = default)
    {
        HttpResponseMessage response;
        try
        {
            response = await http.PostAsJsonAsync("/api/basket/add", item, ct);
        }
        catch (HttpRequestException)
        {
            throw new ServiceUnavailableException("BasketService");
        }

        if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
        {
            var message = await response.Content.ReadAsStringAsync(ct);
            throw new RemoteValidationException(message);
        }

        if (!response.IsSuccessStatusCode)
            throw new ServiceUnavailableException("BasketService");
    }

    public async Task ClearBasketAsync(CancellationToken ct = default)
    {
        HttpResponseMessage response;
        try
        {
            response = await http.DeleteAsync("/api/basket", ct);
        }
        catch (HttpRequestException)
        {
            throw new ServiceUnavailableException("BasketService");
        }

        if (!response.IsSuccessStatusCode)
            throw new ServiceUnavailableException("BasketService");
    }
}
