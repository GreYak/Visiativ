using System.Net.Http.Json;

namespace Visiativ.Web;

public interface IVisiativApiClient
{
    Task<ProductResponse[]> GetProductsAsync(CancellationToken cancellationToken = default);
    Task<BasketResult> GetBasketAsync(CancellationToken cancellationToken = default);
    Task<HttpResponseMessage> AddItemAsync(Guid productId, int quantity, CancellationToken cancellationToken = default);
    Task<HttpResponseMessage> ClearBasketAsync(CancellationToken cancellationToken = default);
    Task<HttpResponseMessage> PayBasketAsync(CancellationToken cancellationToken = default);
}

public class VisiativApiClient(HttpClient httpClient) : IVisiativApiClient
{
    public async Task<ProductResponse[]> GetProductsAsync(CancellationToken cancellationToken = default)
        => await httpClient.GetFromJsonAsync<ProductResponse[]>("/products", cancellationToken) ?? [];

    public async Task<BasketResult> GetBasketAsync(CancellationToken cancellationToken = default)
    {
        var response = await httpClient.GetAsync("/basket", cancellationToken);
        var isPartial = (int)response.StatusCode == 207;
        if (!response.IsSuccessStatusCode && !isPartial)
            throw new HttpRequestException($"Erreur {(int)response.StatusCode} lors du chargement du panier.");
        var items = await response.Content.ReadFromJsonAsync<BasketItemResponse[]>(cancellationToken) ?? [];
        return new BasketResult(items, isPartial);
    }

    public async Task<HttpResponseMessage> AddItemAsync(Guid productId, int quantity, CancellationToken cancellationToken = default)
        => await httpClient.PostAsJsonAsync("/basket/items", new { productId, quantity }, cancellationToken);

    public async Task<HttpResponseMessage> ClearBasketAsync(CancellationToken cancellationToken = default)
        => await httpClient.DeleteAsync("/basket", cancellationToken);

    public async Task<HttpResponseMessage> PayBasketAsync(CancellationToken cancellationToken = default)
        => await httpClient.PostAsync("/basket/pay", null, cancellationToken);
}

public record ProductResponse(Guid Id, string Name, string Description, decimal Price, int Stock);
public record BasketItemResponse(Guid ProductId, string Name, string Description, decimal Price, int Quantity, int Stock);
public record BasketResult(BasketItemResponse[] Items, bool IsPartial);
public record PaymentResponse(decimal Total);
