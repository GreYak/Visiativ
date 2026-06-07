namespace Visiativ.Web;

public class BasketApiClient(HttpClient httpClient)
{
    public async Task<BasketItemResponse[]> GetBasketAsync(CancellationToken cancellationToken = default)
    {
        return await httpClient.GetFromJsonAsync<BasketItemResponse[]>("/basket", cancellationToken)
               ?? [];
    }

    public async Task<HttpResponseMessage> AddItemAsync(Guid productId, int quantity, CancellationToken cancellationToken = default)
    {
        return await httpClient.PostAsJsonAsync("/basket/items", new { productId, quantity }, cancellationToken);
    }

    public async Task<HttpResponseMessage> ClearBasketAsync(CancellationToken cancellationToken = default)
    {
        return await httpClient.DeleteAsync("/basket", cancellationToken);
    }
}

public record BasketItemResponse(Guid ProductId, string Name, decimal Price, int Quantity);
