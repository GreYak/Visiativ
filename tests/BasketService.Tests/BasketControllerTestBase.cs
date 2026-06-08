using BasketService.Controllers;
using BasketService.Domain;
using BasketService.Domain.Ports.Spi;
using BasketService.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Web.Http;
using System.Web.Http.Dependencies;

namespace BasketService.Tests
{
    public abstract class BasketControllerTestBase
    {
        protected const string BaseUrl = "http://localhost/api/basket";

        protected static HttpClient CreateClient(IBasketItemRepository repository)
        {
            var config = new HttpConfiguration();
            WebApiConfig.Register(config);
            config.DependencyResolver = new TestDependencyResolver(repository);
            return new HttpClient(new HttpServer(config));
        }

        protected static System.Threading.Tasks.Task<HttpResponseMessage> PostItem(
            HttpClient client, BasketItem item, int? limitMax = null)
        {
            var body = new
            {
                productId = item.ProductId,
                quantity  = item.Quantity,
                limitMax
            };
            var content = new StringContent(
                JsonConvert.SerializeObject(body), Encoding.UTF8, "application/json");
            return client.PostAsync(BaseUrl + "/add", content);
        }

        private sealed class TestDependencyResolver : IDependencyResolver
        {
            private readonly IBasketItemRepository _repository;

            public TestDependencyResolver(IBasketItemRepository repository)
                => _repository = repository;

            // Un nouveau contrôleur est créé à chaque requête car ApiController
            // est IDisposable et Web API le dispose après chaque appel.
            public object GetService(Type serviceType)
            {
                if (serviceType != typeof(BasketController)) return null;
                return new BasketController(
                    new GetBasket(_repository),
                    new AddItemToBasket(_repository),
                    new DeleteBasket(_repository));
            }

            public IEnumerable<object> GetServices(Type serviceType)
                => new object[0];

            public IDependencyScope BeginScope() => this;

            public void Dispose() { }
        }
    }
}
