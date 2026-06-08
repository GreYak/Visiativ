using System;

namespace BasketService.Domain.Model
{
    /// <summary>Modèle métier représentant une ligne du panier : (ProductId, Quantity).</summary>
    public class BasketItem
    {
        public Guid ProductId { get; }
        public int  Quantity  { get; }

        public BasketItem(Guid productId, int quantity)
        {
            ProductId = productId;
            Quantity  = quantity;
        }
    }
}
