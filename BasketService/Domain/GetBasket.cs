using BasketService.Domain.Ports.Spi;
using BasketService.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BasketService.Domain
{
    public class GetBasket
    {
        private readonly IBasketItemRepository _repository;

        public GetBasket(IBasketItemRepository repository)
        {
            _repository = repository;
        }

        public Task<IEnumerable<BasketItem>> HandleAsync()
        {
            return Task.FromResult(_repository.Get());
        }
    }
}
