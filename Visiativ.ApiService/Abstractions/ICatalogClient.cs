using Visiativ.ApiService.Models;

namespace Visiativ.ApiService.Abstractions;

public interface ICatalogClient
{
    Task<IEnumerable<ProductResponse>> GetAllProductsAsync(CancellationToken ct = default);
    Task<ProductResponse?> GetProductByIdAsync(Guid id, CancellationToken ct = default);
}
