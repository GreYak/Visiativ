using BasketService.Domain.Ports.Spi;
using BasketService.Models;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace BasketService.Domain
{
    public class AddItemToBasket
    {
        private readonly IBasketItemRepository _repository;

        public AddItemToBasket(IBasketItemRepository repository)
        {
            _repository = repository;
        }

        public Task HandleAsync(BasketItem item, int? limitMax = null)
        {
            if (item.Quantity <= 0)
                throw new ArgumentException("La quantité doit être supérieure à zéro.", nameof(item));

            // Si le produit existe déjà dans le panier, on additionne les quantités.
            // Le prix est toujours celui du nouvel ajout.
            var existing = _repository.Get()
                .FirstOrDefault(i => i.ProductId == item.ProductId);

            var finalQuantity = existing != null
                ? existing.Quantity + item.Quantity
                : item.Quantity;

            if (limitMax.HasValue && finalQuantity > limitMax.Value)
                throw new InvalidOperationException(
                    $"Oversize the limit: final quantity ({finalQuantity}) exceeds the maximum allowed ({limitMax.Value}).");

            var finalItem = new BasketItem(item.ProductId, finalQuantity);

            _repository.EnsureBasketItem(finalItem);
            return Task.CompletedTask;
        }
    }
}
