using System;

namespace BasketService.Models
{
    /// <summary>
    /// Corps de la requête POST /api/basket/add.
    /// Regroupe les données de l'item et le paramètre optionnel de limite de quantité.
    /// </summary>
    public class AddItemRequest
    {
        public Guid    ProductId { get; set; }
        public string  Name      { get; set; }
        public decimal Price     { get; set; }
        public int     Quantity  { get; set; }

        /// <summary>
        /// Limite maximale de quantité (après accumulation).
        /// Null = pas de limite. Négatif = requête invalide (400).
        /// </summary>
        public int? LimitMax { get; set; }

        public BasketItem ToBasketItem() =>
            new BasketItem(ProductId, Name, Price, Quantity);
    }
}
