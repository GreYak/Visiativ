using BasketService.Domain.Ports.Spi;
using NSubstitute;
using NUnit.Framework;
using System;
using System.Net;
using System.Threading.Tasks;

namespace BasketService.Tests
{
    [TestFixture]
    public class BasketControllerDeleteTests : BasketControllerTestBase
    {
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
                Assert.That(body, Does.Contain("error"));
            }
        }
    }
}
