using BasketService.Domain.Ports.Spi;
using BasketService.Domain.Model;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BasketService.Domain
{
    public class GetBasketUseCase
    {
        private readonly IBasketItemRepository _repository;

        public GetBasketUseCase(IBasketItemRepository repository)
        {
            _repository = repository;
        }

        public Task<IEnumerable<BasketItem>> HandleAsync()
        {
            return Task.FromResult(_repository.Get());
        }
    }
}
