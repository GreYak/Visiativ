using BasketService.Domain.Model;
using System;

namespace BasketService.Models
{
    /// <summary>
    /// Corps de la requête POST /api/basket/add.
    /// </summary>
    public class AddItemRequest
    {
        public Guid ProductId { get; set; }
        public int  Quantity  { get; set; }

        /// <summary>
        /// Limite maximale de quantité (après accumulation).
        /// Null = pas de limite. Négatif ou zéro = requête invalide (400).
        /// </summary>
        public int? LimitMax { get; set; }

        public BasketItem ToBasketItem() => new BasketItem(ProductId, Quantity);
    }
}
