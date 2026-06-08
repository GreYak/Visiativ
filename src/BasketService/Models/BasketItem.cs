using System;
using Newtonsoft.Json;

namespace BasketService.Models
{
    public class BasketItem
    {
        public Guid ProductId { get; private set; }
        public int  Quantity  { get; private set; }

        [JsonConstructor]
        public BasketItem(Guid productId, int quantity)
        {
            ProductId = productId;
            Quantity  = quantity;
        }
    }
}
