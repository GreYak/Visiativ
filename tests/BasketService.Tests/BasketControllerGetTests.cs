using BasketService.Domain.Ports.Spi;
using BasketService.Models;
using Newtonsoft.Json;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;

namespace BasketService.Tests
{
    [TestFixture]
    public class BasketControllerGetTests : BasketControllerTestBase
    {
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
                new BasketItem(Guid.NewGuid(), 1),
                new BasketItem(Guid.NewGuid(), 2)
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
                Assert.That(body, Does.Contain("error"));
            }
        }
    }
}
