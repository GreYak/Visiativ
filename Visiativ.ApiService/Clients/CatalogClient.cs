using Visiativ.ApiService.Abstractions;
using Visiativ.ApiService.Models;

namespace Visiativ.ApiService.Clients;

public class CatalogClient(HttpClient http) : ICatalogClient
{
    public async Task<IEnumerable<ProductResponse>> GetAllProductsAsync(CancellationToken ct = default)
    {
        var result = await http.GetFromJsonAsync<IEnumerable<ProductResponse>>("/products", ct);
        return result ?? [];
    }

    public async Task<ProductResponse?> GetProductByIdAsync(Guid id, CancellationToken ct = default)
    {
        var response = await http.GetAsync($"/products/{id}", ct);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            return null;

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<ProductResponse>(ct);
    }
}
