using BasketService.Domain.Model;
using System.Collections.Generic;

namespace BasketService.Domain.Ports.Spi
{
    public interface IBasketItemRepository
    {
        IEnumerable<BasketItem> Get();
        void EnsureBasketItem(BasketItem item);
        void Clear();
    }
}
