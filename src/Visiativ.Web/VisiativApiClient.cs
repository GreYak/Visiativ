using System.Net.Http.Json;

namespace Visiativ.Web;

public interface IVisiativApiClient
{
    Task<ProductResponse[]> GetProductsAsync(CancellationToken cancellationToken = default);
    Task<BasketItemResponse[]> GetBasketAsync(CancellationToken cancellationToken = default);
    Task<HttpResponseMessage> AddItemAsync(Guid productId, int quantity, CancellationToken cancellationToken = default);
    Task<HttpResponseMessage> ClearBasketAsync(CancellationToken cancellationToken = default);
}

public class VisiativApiClient(HttpClient httpClient) : IVisiativApiClient
{
    public async Task<ProductResponse[]> GetProductsAsync(CancellationToken cancellationToken = default)
        => await httpClient.GetFromJsonAsync<ProductResponse[]>("/products", cancellationToken) ?? [];

    public async Task<BasketItemResponse[]> GetBasketAsync(CancellationToken cancellationToken = default)
        => await httpClient.GetFromJsonAsync<BasketItemResponse[]>("/basket", cancellationToken) ?? [];

    public async Task<HttpResponseMessage> AddItemAsync(Guid productId, int quantity, CancellationToken cancellationToken = default)
        => await httpClient.PostAsJsonAsync("/basket/items", new { productId, quantity }, cancellationToken);

    public async Task<HttpResponseMessage> ClearBasketAsync(CancellationToken cancellationToken = default)
        => await httpClient.DeleteAsync("/basket", cancellationToken);
}

public record ProductResponse(Guid Id, string Name, string Description, decimal Price, int Stock);
public record BasketItemResponse(Guid ProductId, string Name, decimal Price, int Quantity);
