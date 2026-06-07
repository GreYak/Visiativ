namespace Visiativ.Web;

public class CatalogApiClient(HttpClient httpClient)
{
    public async Task<ProductResponse[]> GetProductsAsync(CancellationToken cancellationToken = default)
    {
        return await httpClient.GetFromJsonAsync<ProductResponse[]>("/products", cancellationToken)
               ?? [];
    }
}

public record ProductResponse(Guid Id, string Name, string Description, decimal Price, int Stock);
