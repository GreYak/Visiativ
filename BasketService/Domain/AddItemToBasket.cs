using BasketService.Domain.Ports.Spi;
using BasketService.Models;
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

        public Task HandleAsync(BasketItem item)
        {
            _repository.Add(item);
            return Task.CompletedTask;
        }
    }
}
