using BasketService.Infrastructure;
using System.Threading.Tasks;

namespace BasketService.Domain
{
    public class DeleteBasket
    {
        private readonly BasketItemRepository _repository;

        public DeleteBasket(BasketItemRepository repository)
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
