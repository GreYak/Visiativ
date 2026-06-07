using NSubstitute;
using NSubstitute.ClearExtensions;
using NUnit.Framework;
using System.Net;
using System.Net.Http.Json;
using Visiativ.ApiService.Models;

namespace Visiativ.ApiService.Tests;

/// <summary>GET /basket</summary>
[TestFixture]
public class GetBasketTests
{
    private ApiServiceWebApplicationFactory _factory = null!;
    private HttpClient _client = null!;

    [SetUp]
    public void SetUp()
    {
        _factory = new ApiServiceWebApplicationFactory();
        _client  = _factory.CreateClient();
        _factory.BasketClient.ClearSubstitute();
    }

    [TearDown]
    public void TearDown()
    {
        _client.Dispose();
        _factory.Dispose();
    }

    [Test]
    public async Task Returns200_WithEmptyBasket()
    {
        _factory.BasketClient
            .GetBasketAsync(Arg.Any<CancellationToken>())
            .Returns([]);

        var response = await _client.GetAsync("/basket");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var body = await response.Content.ReadFromJsonAsync<List<BasketItem>>();
        Assert.That(body, Is.Empty);
    }

    [Test]
    public async Task Returns200_WithItems()
    {
        var items = new List<BasketItem>
        {
            new(Guid.NewGuid(), "Laptop", 999.99m, 1),
            new(Guid.NewGuid(), "Mouse",   29.99m, 2)
        };
        _factory.BasketClient
            .GetBasketAsync(Arg.Any<CancellationToken>())
            .Returns(items);

        var response = await _client.GetAsync("/basket");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var body = await response.Content.ReadFromJsonAsync<List<BasketItem>>();
        Assert.That(body, Has.Count.EqualTo(2));
    }

    [Test]
    public async Task Returns500_WhenClientThrows()
    {
        _factory.BasketClient
            .GetBasketAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromException<IEnumerable<BasketItem>>(
                new HttpRequestException("BasketService unavailable")));

        var response = await _client.GetAsync("/basket");

        Assert.That((int)response.StatusCode, Is.EqualTo(500));
    }
}
