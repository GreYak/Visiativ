using BasketService.Domain.Model;
using BasketService.Domain.Ports.Spi;
using System.Collections.Generic;
using System.Linq;

namespace BasketService.Tests
{
    internal sealed class InMemoryBasketItemRepository : IBasketItemRepository
    {
        private readonly List<BasketItem> _items = new List<BasketItem>();

        public IEnumerable<BasketItem> Get() => _items;

        public void EnsureBasketItem(BasketItem item)
        {
            var existing = _items.FirstOrDefault(i => i.ProductId == item.ProductId);
            if (existing != null) _items.Remove(existing);
            _items.Add(item);
        }

        public void Clear() => _items.Clear();
    }
}
