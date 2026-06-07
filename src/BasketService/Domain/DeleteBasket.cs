using BasketService.Domain.Ports.Spi;
using System.Threading.Tasks;

namespace BasketService.Domain
{
    public class DeleteBasket
    {
        private readonly IBasketItemRepository _repository;

        public DeleteBasket(IBasketItemRepository repository)
        {
            _repository = repository;
        }

        public Task HandleAsync()
        {
            _repository.Clear();
            return Task.CompletedTask;
        }
    }
}
