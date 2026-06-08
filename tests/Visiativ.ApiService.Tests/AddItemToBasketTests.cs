using NSubstitute;
using NSubstitute.ClearExtensions;
using NUnit.Framework;
using System.Net;
using System.Net.Http.Json;
using Visiativ.ApiService.Exceptions;
using Visiativ.ApiService.Models;

namespace Visiativ.ApiService.Tests;

/// <summary>POST /basket/items</summary>
[TestFixture]
public class AddItemToBasketTests
{
    private ApiServiceWebApplicationFactory _factory = null!;
    private HttpClient _client = null!;

    [SetUp]
    public void SetUp()
    {
        _factory = new ApiServiceWebApplicationFactory();
        _client  = _factory.CreateClient();
        _factory.CatalogClient.ClearSubstitute();
        _factory.BasketClient.ClearSubstitute();
    }

    [TearDown]
    public void TearDown()
    {
        _client.Dispose();
        _factory.Dispose();
    }

    [Test]
    public async Task Returns200_WhenProductExistsAndStockSufficient()
    {
        var productId = Guid.NewGuid();
        _factory.CatalogClient
            .GetProductByIdAsync(productId, Arg.Any<CancellationToken>())
            .Returns(new ProductResponse(productId, "Laptop", "High-end laptop", 999.99m, Stock: 10));
        _factory.BasketClient
            .AddItemAsync(Arg.Any<BasketItem>(), Arg.Any<int?>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var response = await _client.PostAsJsonAsync("/basket/items",
            new AddItemRequest(productId, Quantity: 3));

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
    }

    [Test]
    public async Task Returns400_WhenProductNotFound()
    {
        var unknownId = Guid.NewGuid();
        _factory.CatalogClient
            .GetProductByIdAsync(unknownId, Arg.Any<CancellationToken>())
            .Returns((ProductResponse?)null);

        var response = await _client.PostAsJsonAsync("/basket/items",
            new AddItemRequest(unknownId, Quantity: 1));

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }

    [Test]
    public async Task Returns400_WhenStockInsufficient()
    {
        var productId = Guid.NewGuid();
        _factory.CatalogClient
            .GetProductByIdAsync(productId, Arg.Any<CancellationToken>())
            .Returns(new ProductResponse(productId, "Laptop", "High-end laptop", 999.99m, Stock: 2));

        var response = await _client.PostAsJsonAsync("/basket/items",
            new AddItemRequest(productId, Quantity: 5));

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }

    [Test]
    public async Task Returns400_WhenStockIsZero()
    {
        var productId = Guid.NewGuid();
        _factory.CatalogClient
            .GetProductByIdAsync(productId, Arg.Any<CancellationToken>())
            .Returns(new ProductResponse(productId, "Laptop", "High-end laptop", 999.99m, Stock: 0));

        var response = await _client.PostAsJsonAsync("/basket/items",
            new AddItemRequest(productId, Quantity: 1));

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }

    /// <summary>
    /// Le BasketService a rejeté l'item (quantité invalide) : le BFF propage le 400.
    /// </summary>
    [Test]
    public async Task Returns400_WhenBasketRejectsItem_DueToInvalidQuantity()
    {
        var productId = Guid.NewGuid();
        _factory.CatalogClient
            .GetProductByIdAsync(productId, Arg.Any<CancellationToken>())
            .Returns(new ProductResponse(productId, "Laptop", "High-end laptop", 999.99m, Stock: 10));
        _factory.BasketClient
            .AddItemAsync(Arg.Any<BasketItem>(), Arg.Any<int?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException(
                new RemoteValidationException("La quantité doit être supérieure à zéro.")));

        var response = await _client.PostAsJsonAsync("/basket/items",
            new AddItemRequest(productId, Quantity: 3));

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }

    /// <summary>
    /// Le BasketService indique que la quantité accumulée dépasse le stock : le BFF propage le 409.
    /// </summary>
    [Test]
    public async Task Returns409_WhenBasketRejectsItem_DueToStockLimitExceeded()
    {
        var productId = Guid.NewGuid();
        _factory.CatalogClient
            .GetProductByIdAsync(productId, Arg.Any<CancellationToken>())
            .Returns(new ProductResponse(productId, "Laptop", "High-end laptop", 999.99m, Stock: 5));
        _factory.BasketClient
            .AddItemAsync(Arg.Any<BasketItem>(), Arg.Any<int?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException(
                new RemoteConflictException("Oversize the limit: final quantity (7) exceeds the maximum allowed (5).")));

        var response = await _client.PostAsJsonAsync("/basket/items",
            new AddItemRequest(productId, Quantity: 3));

        Assert.That((int)response.StatusCode, Is.EqualTo(409));
    }

    [Test]
    public async Task Returns503_WhenCatalogUnavailable()
    {
        var productId = Guid.NewGuid();
        _factory.CatalogClient
            .GetProductByIdAsync(productId, Arg.Any<CancellationToken>())
            .Returns(Task.FromException<ProductResponse?>(
                new ServiceUnavailableException("CatalogService")));

        var response = await _client.PostAsJsonAsync("/basket/items",
            new AddItemRequest(productId, Quantity: 1));

        Assert.That((int)response.StatusCode, Is.EqualTo(503));
    }

    [Test]
    public async Task Returns503_WhenBasketUnavailable()
    {
        var productId = Guid.NewGuid();
        _factory.CatalogClient
            .GetProductByIdAsync(productId, Arg.Any<CancellationToken>())
            .Returns(new ProductResponse(productId, "Laptop", "High-end laptop", 999.99m, Stock: 10));
        _factory.BasketClient
            .AddItemAsync(Arg.Any<BasketItem>(), Arg.Any<int?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException(new ServiceUnavailableException("BasketService")));

        var response = await _client.PostAsJsonAsync("/basket/items",
            new AddItemRequest(productId, Quantity: 1));

        Assert.That((int)response.StatusCode, Is.EqualTo(503));
    }

    [Test]
    public async Task Returns503Body_ContainsServiceName_WhenCatalogUnavailable()
    {
        var productId = Guid.NewGuid();
        _factory.CatalogClient
            .GetProductByIdAsync(productId, Arg.Any<CancellationToken>())
            .Returns(Task.FromException<ProductResponse?>(
                new ServiceUnavailableException("CatalogService")));

        var response = await _client.PostAsJsonAsync("/basket/items",
            new AddItemRequest(productId, Quantity: 1));
        var body = await response.Content.ReadAsStringAsync();

        Assert.That(body, Does.Contain("CatalogService"));
    }
}
