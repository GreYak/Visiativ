using BasketService.Infrastructure;
using BasketService.Models;
using System.Threading.Tasks;

namespace BasketService.Domain
{
    public class AddItemToBasket
    {
        private readonly BasketItemRepository _repository;

        public AddItemToBasket(BasketItemRepository repository)
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
