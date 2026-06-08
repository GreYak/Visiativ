using BasketService.Domain.Ports.Spi;
using System.Threading.Tasks;

namespace BasketService.Domain
{
    public class DeleteBasketUseCase
    {
        private readonly IBasketItemRepository _repository;

        public DeleteBasketUseCase(IBasketItemRepository repository)
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
