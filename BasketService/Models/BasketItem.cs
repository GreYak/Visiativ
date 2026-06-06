using System;
using Newtonsoft.Json;

namespace BasketService.Models
{
    public class BasketItem
    {
        public Guid ProductId { get; private set; }
        public string Name { get; private set; }
        public decimal Price { get; private set; }
        public int Quantity { get; private set; }

        [JsonConstructor]
        public BasketItem(Guid productId, string name, decimal price, int quantity)
        {
            ProductId = productId;
            Name      = name;
            Price     = price;
            Quantity  = quantity;
        }
    }
}