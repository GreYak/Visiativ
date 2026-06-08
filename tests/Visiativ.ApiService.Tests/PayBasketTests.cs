using NSubstitute;
using NSubstitute.ClearExtensions;
using NUnit.Framework;
using System.Net;
using System.Net.Http.Json;
using Visiativ.ApiService.Clients;
using Visiativ.ApiService.Exceptions;
using Visiativ.ApiService.Models;

namespace Visiativ.ApiService.Tests;

/// <summary>POST /basket/pay</summary>
[TestFixture]
public class PayBasketTests
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
    public async Task Returns200_WithCorrectTotal_WhenBasketIsValid()
    {
        // Laptop ×2 = 1999.98 + Mouse ×3 = 89.97 → total = 2089.95
        _factory.BasketClient
            .GetBasketAsync(Arg.Any<CancellationToken>())
            .Returns(new[]
            {
                new BasketItemExt(LaptopId, Quantity: 2),
                new BasketItemExt(MouseId,  Quantity: 3)
            });
        _factory.CatalogClient
            .GetAllProductsAsync(Arg.Any<CancellationToken>())
            .Returns(new[]
            {
                new ProductExt(LaptopId, "Laptop Pro", "Portable haute gamme", 999.99m, Stock: 10),
                new ProductExt(MouseId,  "Souris USB",  "Souris sans fil",       29.99m, Stock:  5)
            });
        _factory.BasketClient
            .ClearBasketAsync(Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var response = await _client.PostAsync("/basket/pay", null);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var body = await response.Content.ReadFromJsonAsync<PaymentDto>();
        Assert.That(body!.Total, Is.EqualTo(2089.95m));

        // Le panier doit avoir été vidé après paiement
        await _factory.BasketClient.Received(1).ClearBasketAsync(Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Returns400_WhenProductMissingFromCatalog()
    {
        var unknownId = Guid.NewGuid();
        _factory.BasketClient
            .GetBasketAsync(Arg.Any<CancellationToken>())
            .Returns(new[]
            {
                new BasketItemExt(LaptopId, Quantity: 1),
                new BasketItemExt(unknownId, Quantity: 2)   // absent du catalogue
            });
        _factory.CatalogClient
            .GetAllProductsAsync(Arg.Any<CancellationToken>())
            .Returns(new[]
            {
                new ProductExt(LaptopId, "Laptop Pro", "Portable haute gamme", 999.99m, Stock: 10)
            });

        var response = await _client.PostAsync("/basket/pay", null);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        // Le panier ne doit PAS être vidé
        await _factory.BasketClient.DidNotReceive().ClearBasketAsync(Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Returns400_WhenQuantityExceedsStock()
    {
        _factory.BasketClient
            .GetBasketAsync(Arg.Any<CancellationToken>())
            .Returns(new[] { new BasketItemExt(LaptopId, Quantity: 5) });
        _factory.CatalogClient
            .GetAllProductsAsync(Arg.Any<CancellationToken>())
            .Returns(new[]
            {
                new ProductExt(LaptopId, "Laptop Pro", "Portable haute gamme", 999.99m, Stock: 3)
            });

        var response = await _client.PostAsync("/basket/pay", null);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        var body = await response.Content.ReadAsStringAsync();
        Assert.That(body, Does.Contain("Stock insuffisant"));
        // Le panier ne doit PAS être vidé
        await _factory.BasketClient.DidNotReceive().ClearBasketAsync(Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Returns503_WhenBasketUnavailableOnFetch()
    {
        _factory.BasketClient
            .GetBasketAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromException<IEnumerable<BasketItemExt>>(
                new ServiceUnavailableException("BasketService")));

        var response = await _client.PostAsync("/basket/pay", null);

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

        var response = await _client.PostAsync("/basket/pay", null);

        Assert.That((int)response.StatusCode, Is.EqualTo(503));
    }

    [Test]
    public async Task Returns503_WhenBasketUnavailableOnClear()
    {
        _factory.BasketClient
            .GetBasketAsync(Arg.Any<CancellationToken>())
            .Returns(new[] { new BasketItemExt(LaptopId, Quantity: 1) });
        _factory.CatalogClient
            .GetAllProductsAsync(Arg.Any<CancellationToken>())
            .Returns(new[]
            {
                new ProductExt(LaptopId, "Laptop Pro", "Portable haute gamme", 999.99m, Stock: 10)
            });
        _factory.BasketClient
            .ClearBasketAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromException(new ServiceUnavailableException("BasketService")));

        var response = await _client.PostAsync("/basket/pay", null);

        Assert.That((int)response.StatusCode, Is.EqualTo(503));
    }
}
