using Visiativ.ApiService.Clients;

namespace Visiativ.ApiService.Abstractions;

public interface ICatalogClient
{
    Task<IEnumerable<ProductExt>> GetAllProductsAsync(CancellationToken ct = default);
    Task<ProductExt?> GetProductByIdAsync(Guid id, CancellationToken ct = default);
}
