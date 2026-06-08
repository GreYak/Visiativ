using BasketService.Domain.Model;
using BasketService.Domain.Ports.Spi;
using NSubstitute;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace BasketService.Tests
{
    [TestFixture]
    public class BasketControllerAddTests : BasketControllerTestBase
    {
        [Test]
        public async Task Add_Returns200_WhenItemIsValid()
        {
            var repo = Substitute.For<IBasketItemRepository>();
            repo.Get().Returns(new List<BasketItem>());

            using (var client = CreateClient(repo))
            {
                var response = await PostItem(client, new BasketItem(Guid.NewGuid(), 1));

                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            }
        }

        [Test]
        public async Task Add_Returns400_WhenQuantityIsNegative()
        {
            var repo = Substitute.For<IBasketItemRepository>();

            using (var client = CreateClient(repo))
            {
                var response = await PostItem(client, new BasketItem(Guid.NewGuid(), -1));

                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
            }
        }

        [Test]
        public async Task Add_Returns400_WhenQuantityIsZero()
        {
            var repo = Substitute.For<IBasketItemRepository>();

            using (var client = CreateClient(repo))
            {
                var response = await PostItem(client, new BasketItem(Guid.NewGuid(), 0));

                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
            }
        }

        [Test]
        public async Task Add_Returns500_WhenRepositoryThrows()
        {
            var repo = Substitute.For<IBasketItemRepository>();
            repo.Get().Returns(new List<BasketItem>());
            repo.When(r => r.EnsureBasketItem(Arg.Any<BasketItem>())).Throw(new Exception("DB error"));

            using (var client = CreateClient(repo))
            {
                var response = await PostItem(client, new BasketItem(Guid.NewGuid(), 1));

                Assert.That((int)response.StatusCode, Is.EqualTo(500));
            }
        }

        [Test]
        public async Task Add_Returns500WithJsonBody_WhenRepositoryThrows()
        {
            var repo = Substitute.For<IBasketItemRepository>();
            repo.Get().Returns(new List<BasketItem>());
            repo.When(r => r.EnsureBasketItem(Arg.Any<BasketItem>())).Throw(new Exception("DB error"));

            using (var client = CreateClient(repo))
            {
                var response = await PostItem(client, new BasketItem(Guid.NewGuid(), 1));
                var body = await response.Content.ReadAsStringAsync();

                Assert.That((int)response.StatusCode, Is.EqualTo(500));
                Assert.That(body, Does.Contain("error"));
            }
        }

        [Test]
        public async Task Add_NewItem_StoresOriginalQuantity()
        {
            var repo = new InMemoryBasketItemRepository();
            var productId = Guid.NewGuid();

            using (var client = CreateClient(repo))
            {
                var response = await PostItem(client, new BasketItem(productId, 2));

                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
                Assert.That(repo.Get().Single(i => i.ProductId == productId).Quantity, Is.EqualTo(2));
            }
        }

        [Test]
        public async Task Add_SameItemTwice_AccumulatesQuantities()
        {
            var repo = new InMemoryBasketItemRepository();
            var productId = Guid.NewGuid();

            using (var client = CreateClient(repo))
            {
                await PostItem(client, new BasketItem(productId, 2));
                await PostItem(client, new BasketItem(productId, 3));

                Assert.That(repo.Get().Single(i => i.ProductId == productId).Quantity, Is.EqualTo(5));
            }
        }

        [Test]
        public async Task Add_WithNegativeLimitMax_Returns400()
        {
            var repo = new InMemoryBasketItemRepository();

            using (var client = CreateClient(repo))
            {
                var response = await PostItem(client, new BasketItem(Guid.NewGuid(), 1), limitMax: -1);

                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
            }
        }

        [Test]
        public async Task Add_WithZeroLimitMax_Returns400()
        {
            var repo = new InMemoryBasketItemRepository();

            using (var client = CreateClient(repo))
            {
                var response = await PostItem(client, new BasketItem(Guid.NewGuid(), 1), limitMax: 0);

                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
            }
        }

        [Test]
        public async Task Add_WithLimitMax_WhenNewItemIsWithinLimit_Returns200()
        {
            var repo = new InMemoryBasketItemRepository();
            var productId = Guid.NewGuid();

            using (var client = CreateClient(repo))
            {
                var response = await PostItem(client, new BasketItem(productId, 5), limitMax: 5);

                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
                Assert.That(repo.Get().Single(i => i.ProductId == productId).Quantity, Is.EqualTo(5));
            }
        }

        [Test]
        public async Task Add_WithLimitMax_WhenNewItemExceedsLimit_Returns409()
        {
            var repo = new InMemoryBasketItemRepository();

            using (var client = CreateClient(repo))
            {
                var response = await PostItem(client, new BasketItem(Guid.NewGuid(), 5), limitMax: 3);
                var body = await response.Content.ReadAsStringAsync();

                Assert.That((int)response.StatusCode, Is.EqualTo(409));
                Assert.That(body, Does.Contain("dépasse le stock maximum autorisé").IgnoreCase);
            }
        }

        [Test]
        public async Task Add_WithLimitMax_WhenAccumulatedQuantityIsWithinLimit_Returns200()
        {
            var repo = new InMemoryBasketItemRepository();
            var productId = Guid.NewGuid();

            using (var client = CreateClient(repo))
            {
                await PostItem(client, new BasketItem(productId, 2), limitMax: 10);
                var response = await PostItem(client, new BasketItem(productId, 3), limitMax: 10);

                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
                Assert.That(repo.Get().Single(i => i.ProductId == productId).Quantity, Is.EqualTo(5));
            }
        }

        [Test]
        public async Task Add_WithLimitMax_WhenAccumulatedQuantityExceedsLimit_Returns409()
        {
            var repo = new InMemoryBasketItemRepository();
            var productId = Guid.NewGuid();

            using (var client = CreateClient(repo))
            {
                await PostItem(client, new BasketItem(productId, 2), limitMax: 4);
                var response = await PostItem(client, new BasketItem(productId, 3), limitMax: 4);
                var body = await response.Content.ReadAsStringAsync();

                Assert.That((int)response.StatusCode, Is.EqualTo(409));
                Assert.That(body, Does.Contain("dépasse le stock maximum autorisé").IgnoreCase);
            }
        }
    }
}
