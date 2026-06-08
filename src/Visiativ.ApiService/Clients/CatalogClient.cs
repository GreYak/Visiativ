using Visiativ.ApiService.Abstractions;
using Visiativ.ApiService.Exceptions;

namespace Visiativ.ApiService.Clients;

public class CatalogClient(HttpClient http) : ICatalogClient
{
    public async Task<IEnumerable<ProductExt>> GetAllProductsAsync(CancellationToken ct = default)
    {
        try
        {
            var result = await http.GetFromJsonAsync<IEnumerable<ProductExt>>("/products", ct);
            return result ?? [];
        }
        catch (HttpRequestException)
        {
            throw new ServiceUnavailableException("CatalogService");
        }
    }

    public async Task<ProductExt?> GetProductByIdAsync(Guid id, CancellationToken ct = default)
    {
        HttpResponseMessage response;
        try
        {
            response = await http.GetAsync($"/products/{id}", ct);
        }
        catch (HttpRequestException)
        {
            throw new ServiceUnavailableException("CatalogService");
        }

        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            return null;

        if (!response.IsSuccessStatusCode)
            throw new ServiceUnavailableException("CatalogService");

        return await response.Content.ReadFromJsonAsync<ProductExt>(ct);
    }
}
