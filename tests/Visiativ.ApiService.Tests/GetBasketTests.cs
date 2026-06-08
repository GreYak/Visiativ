using NSubstitute;
using NSubstitute.ClearExtensions;
using NUnit.Framework;
using System.Net;
using System.Net.Http.Json;
using Visiativ.ApiService.Clients;
using Visiativ.ApiService.Exceptions;
using Visiativ.ApiService.Models;

namespace Visiativ.ApiService.Tests;

/// <summary>GET /basket</summary>
[TestFixture]
public class GetBasketTests
{
    private ApiServiceWebApplicationFactory _factory = null!;
    private HttpClient _client = null!;

    private static readonly Guid LaptopId = Guid.NewGuid();
    private static readonly Guid MouseId  = Guid.NewGuid();

    [SetUp]
    public void SetUp()
    {
        _factory = new ApiServiceWebApplicationFactory();
        _client  = _factory.CreateClient();
        _factory.BasketClient.ClearSubstitute();
        _factory.CatalogClient.ClearSubstitute();
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
            .Returns(Enumerable.Empty<BasketItemExt>());
        _factory.CatalogClient
            .GetAllProductsAsync(Arg.Any<CancellationToken>())
            .Returns(Enumerable.Empty<ProductExt>());

        var response = await _client.GetAsync("/basket");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var body = await response.Content.ReadFromJsonAsync<List<BasketItemDto>>();
        Assert.That(body, Is.Empty);
    }

    [Test]
    public async Task Returns200_WithItems_EnrichedFromCatalog()
    {
        _factory.BasketClient
            .GetBasketAsync(Arg.Any<CancellationToken>())
            .Returns(new[]
            {
                new BasketItemExt(LaptopId, Quantity: 1),
                new BasketItemExt(MouseId,  Quantity: 2)
            });
        _factory.CatalogClient
            .GetAllProductsAsync(Arg.Any<CancellationToken>())
            .Returns(new[]
            {
                new ProductExt(LaptopId, "Laptop Pro", "High-end laptop", 1099.99m, Stock: 10),
                new ProductExt(MouseId,  "Mouse Pro",  "Wireless mouse",    34.99m, Stock:  5)
            });

        var response = await _client.GetAsync("/basket");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var body = await response.Content.ReadFromJsonAsync<List<BasketItemDto>>();
        Assert.That(body, Has.Count.EqualTo(2));

        // Les infos viennent du catalogue (pas du panier)
        var laptop = body!.Single(i => i.ProductId == LaptopId);
        Assert.That(laptop.Name,        Is.EqualTo("Laptop Pro"));
        Assert.That(laptop.Description, Is.EqualTo("High-end laptop"));
        Assert.That(laptop.Price,       Is.EqualTo(1099.99m));
        Assert.That(laptop.Stock,       Is.EqualTo(10));
        // La quantité vient du panier
        Assert.That(laptop.Quantity,    Is.EqualTo(1));

        var mouse = body!.Single(i => i.ProductId == MouseId);
        Assert.That(mouse.Quantity, Is.EqualTo(2));
    }

    [Test]
    public async Task Returns207_WhenBasketContainsProductAbsentFromCatalog()
    {
        var unknownId = Guid.NewGuid();
        _factory.BasketClient
            .GetBasketAsync(Arg.Any<CancellationToken>())
            .Returns(new[]
            {
                new BasketItemExt(LaptopId,   Quantity: 1),
                new BasketItemExt(unknownId,  Quantity: 3)  // absent du catalogue
            });
        _factory.CatalogClient
            .GetAllProductsAsync(Arg.Any<CancellationToken>())
            .Returns(new[]
            {
                new ProductExt(LaptopId, "Laptop Pro", "High-end laptop", 1099.99m, Stock: 10)
            });

        var response = await _client.GetAsync("/basket");

        Assert.That((int)response.StatusCode, Is.EqualTo(207));
        var body = await response.Content.ReadFromJsonAsync<List<BasketItemDto>>();
        Assert.That(body, Has.Count.EqualTo(1));
        Assert.That(body![0].ProductId, Is.EqualTo(LaptopId));
    }

    [Test]
    public async Task Returns503_WhenBasketUnavailable()
    {
        _factory.BasketClient
            .GetBasketAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromException<IEnumerable<BasketItemExt>>(
                new ServiceUnavailableException("BasketService")));

        var response = await _client.GetAsync("/basket");

        Assert.That((int)response.StatusCode, Is.EqualTo(503));
    }

    [Test]
    public async Task Returns503_WhenCatalogUnavailable()
    {
        _factory.BasketClient
            .GetBasketAsync(Arg.Any<CancellationToken>())
            .Returns(new[] { new BasketItemExt(LaptopId, Quantity: 1) });
        _factory.CatalogClient
            .GetAllProductsAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromException<IEnumerable<ProductExt>>(
                new ServiceUnavailableException("CatalogService")));

        var response = await _client.GetAsync("/basket");

        Assert.That((int)response.StatusCode, Is.EqualTo(503));
    }
}
