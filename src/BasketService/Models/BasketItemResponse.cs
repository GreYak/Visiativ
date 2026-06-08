using BasketService.Domain.Model;
using Newtonsoft.Json;
using System;

namespace BasketService.Models
{
    /// <summary>
    /// DTO exposé par <c>GET /api/basket</c>.
    /// Construit à partir du modèle domaine via <see cref="From"/>.
    /// </summary>
    public class BasketItemResponse
    {
        public Guid ProductId { get; }
        public int  Quantity  { get; }

        [JsonConstructor]
        private BasketItemResponse(Guid productId, int quantity)
        {
            ProductId = productId;
            Quantity  = quantity;
        }

        public static BasketItemResponse From(BasketItem item)
            => new BasketItemResponse(item.ProductId, item.Quantity);
    }
}
