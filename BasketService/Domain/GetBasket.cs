using BasketService.Infrastructure;
using BasketService.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BasketService.Domain
{
    public class GetBasket
    {
        private readonly BasketItemRepository _repository;

        public GetBasket(BasketItemRepository repository)
        {
            _repository = repository;
        }

        public Task<IEnumerable<BasketItem>> HandleAsync()
        {
            return Task.FromResult(_repository.Get());
        }
    }
}
