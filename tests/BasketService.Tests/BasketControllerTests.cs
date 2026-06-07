using BasketService.Controllers;
using BasketService.Domain;
using BasketService.Domain.Ports.Spi;
using BasketService.Models;
using Newtonsoft.Json;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Dependencies;

namespace BasketService.Tests
{
    [TestFixture]
    public class BasketControllerTests
    {
        // ── Helpers ──────────────────────────────────────────────────────────

        private static HttpClient CreateClient(IBasketItemRepository repository)
        {
            var config = new HttpConfiguration();
            WebApiConfig.Register(config);

            var getBasket    = new GetBasket(repository);
            var addItem      = new AddItemToBasket(repository);
            var deleteBasket = new DeleteBasket(repository);

            config.DependencyResolver = new TestDependencyResolver(
                new BasketController(getBasket, addItem, deleteBasket));

            return new HttpClient(new HttpServer(config));
        }

        private const string BaseUrl = "http://localhost/api/basket";

        // ── GET /api/basket ───────────────────────────────────────────────────

        [Test]
        public async Task Get_Returns200_WithEmptyList()
        {
            var repo = Substitute.For<IBasketItemRepository>();
            repo.Get().Returns(new List<BasketItem>());

            using (var client = CreateClient(repo))
            {
                var response = await client.GetAsync(BaseUrl);

                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
                var body = JsonConvert.DeserializeObject<List<BasketItem>>(
                    await response.Content.ReadAsStringAsync());
                Assert.That(body, Is.Empty);
            }
        }

        [Test]
        public async Task Get_Returns200_WithItems()
        {
            var items = new List<BasketItem>
            {
                new BasketItem(Guid.NewGuid(), "Laptop", 999.99m, 1),
                new BasketItem(Guid.NewGuid(), "Mouse",   29.99m, 2)
            };
            var repo = Substitute.For<IBasketItemRepository>();
            repo.Get().Returns(items);

            using (var client = CreateClient(repo))
            {
                var response = await client.GetAsync(BaseUrl);

                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
                var body = JsonConvert.DeserializeObject<List<BasketItem>>(
                    await response.Content.ReadAsStringAsync());
                Assert.That(body, Has.Count.EqualTo(2));
            }
        }

        [Test]
        public async Task Get_Returns500_WhenRepositoryThrows()
        {
            var repo = Substitute.For<IBasketItemRepository>();
            repo.Get().Throws(new Exception("DB error"));

            using (var client = CreateClient(repo))
            {
                var response = await client.GetAsync(BaseUrl);

                Assert.That((int)response.StatusCode, Is.EqualTo(500));
            }
        }

        [Test]
        public async Task Get_Returns500WithJsonBody_WhenRepositoryThrows()
        {
            var repo = Substitute.For<IBasketItemRepository>();
            repo.Get().Throws(new Exception("DB error"));

            using (var client = CreateClient(repo))
            {
                var response = await client.GetAsync(BaseUrl);
                var body = await response.Content.ReadAsStringAsync();

                Assert.That((int)response.StatusCode, Is.EqualTo(500));
                Assert.That(body, Does.Contain("error"),
                    "Le filtre global doit retourner un corps JSON avec une clé 'error'.");
            }
        }

        // ── DELETE /api/basket ────────────────────────────────────────────────

        [Test]
        public async Task Delete_Returns204_OnSuccess()
        {
            var repo = Substitute.For<IBasketItemRepository>();

            using (var client = CreateClient(repo))
            {
                var response = await client.DeleteAsync(BaseUrl);

                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
            }
        }

        [Test]
        public async Task Delete_Returns500_WhenRepositoryThrows()
        {
            var repo = Substitute.For<IBasketItemRepository>();
            repo.When(r => r.Clear()).Throw(new Exception("DB error"));

            using (var client = CreateClient(repo))
            {
                var response = await client.DeleteAsync(BaseUrl);

                Assert.That((int)response.StatusCode, Is.EqualTo(500));
            }
        }

        [Test]
        public async Task Delete_Returns500WithJsonBody_WhenRepositoryThrows()
        {
            var repo = Substitute.For<IBasketItemRepository>();
            repo.When(r => r.Clear()).Throw(new Exception("DB error"));

            using (var client = CreateClient(repo))
            {
                var response = await client.DeleteAsync(BaseUrl);
                var body = await response.Content.ReadAsStringAsync();

                Assert.That((int)response.StatusCode, Is.EqualTo(500));
                Assert.That(body, Does.Contain("error"),
                    "Le filtre global doit retourner un corps JSON avec une clé 'error'.");
            }
        }

        // ── POST /api/basket/add ──────────────────────────────────────────────

        [Test]
        public async Task Add_Returns200_WhenItemIsValid()
        {
            var repo = Substitute.For<IBasketItemRepository>();
            var item = new BasketItem(Guid.NewGuid(), "Keyboard", 89.99m, 1);

            using (var client = CreateClient(repo))
            {
                var content  = new StringContent(
                    JsonConvert.SerializeObject(item),
                    System.Text.Encoding.UTF8, "application/json");
                var response = await client.PostAsync(BaseUrl + "/add", content);

                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            }
        }

        [Test]
        public async Task Add_Returns400_WhenQuantityIsNegative()
        {
            var repo = Substitute.For<IBasketItemRepository>();
            var item = new BasketItem(Guid.NewGuid(), "Keyboard", 89.99m, -1);

            using (var client = CreateClient(repo))
            {
                var content  = new StringContent(
                    JsonConvert.SerializeObject(item),
                    System.Text.Encoding.UTF8, "application/json");
                var response = await client.PostAsync(BaseUrl + "/add", content);

                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
            }
        }

        [Test]
        public async Task Add_Returns400_WhenQuantityIsZero()
        {
            var repo = Substitute.For<IBasketItemRepository>();
            var item = new BasketItem(Guid.NewGuid(), "Keyboard", 89.99m, 0);

            using (var client = CreateClient(repo))
            {
                var content  = new StringContent(
                    JsonConvert.SerializeObject(item),
                    System.Text.Encoding.UTF8, "application/json");
                var response = await client.PostAsync(BaseUrl + "/add", content);

                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
            }
        }

        [Test]
        public async Task Add_Returns500_WhenRepositoryThrows()
        {
            var repo = Substitute.For<IBasketItemRepository>();
            repo.When(r => r.Add(Arg.Any<BasketItem>())).Throw(new Exception("DB error"));
            var item = new BasketItem(Guid.NewGuid(), "Keyboard", 89.99m, 1);

            using (var client = CreateClient(repo))
            {
                var content  = new StringContent(
                    JsonConvert.SerializeObject(item),
                    System.Text.Encoding.UTF8, "application/json");
                var response = await client.PostAsync(BaseUrl + "/add", content);

                Assert.That((int)response.StatusCode, Is.EqualTo(500));
            }
        }

        [Test]
        public async Task Add_Returns500WithJsonBody_WhenRepositoryThrows()
        {
            var repo = Substitute.For<IBasketItemRepository>();
            repo.When(r => r.Add(Arg.Any<BasketItem>())).Throw(new Exception("DB error"));
            var item = new BasketItem(Guid.NewGuid(), "Keyboard", 89.99m, 1);

            using (var client = CreateClient(repo))
            {
                var content  = new StringContent(
                    JsonConvert.SerializeObject(item),
                    System.Text.Encoding.UTF8, "application/json");
                var response = await client.PostAsync(BaseUrl + "/add", content);
                var body = await response.Content.ReadAsStringAsync();

                Assert.That((int)response.StatusCode, Is.EqualTo(500));
                Assert.That(body, Does.Contain("error"),
                    "Le filtre global doit retourner un corps JSON avec une clé 'error'.");
            }
        }

        // ── TestDependencyResolver ────────────────────────────────────────────

        private sealed class TestDependencyResolver : IDependencyResolver
        {
            private readonly BasketController _controller;

            public TestDependencyResolver(BasketController controller)
                => _controller = controller;

            public object GetService(Type serviceType)
                => serviceType == typeof(BasketController) ? _controller : null;

            public IEnumerable<object> GetServices(Type serviceType)
                => new object[0];

            public IDependencyScope BeginScope() => this;

            public void Dispose() { }
        }
    }
}
