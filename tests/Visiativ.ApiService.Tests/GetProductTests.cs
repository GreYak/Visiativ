using NSubstitute;
using NSubstitute.ClearExtensions;
using NUnit.Framework;
using System.Net;
using System.Net.Http.Json;
using Visiativ.ApiService.Models;

namespace Visiativ.ApiService.Tests;

/// <summary>GET /products</summary>
[TestFixture]
public class GetProductsTests
{
    private ApiServiceWebApplicationFactory _factory = null!;
    private HttpClient _client = null!;

    [SetUp]
    public void SetUp()
    {
        _factory = new ApiServiceWebApplicationFactory();
        _client  = _factory.CreateClient();
        _factory.CatalogClient.ClearSubstitute();
    }

    [TearDown]
    public void TearDown()
    {
        _client.Dispose();
        _factory.Dispose();
    }

    [Test]
    public async Task Returns200_WithEmptyList()
    {
        _factory.CatalogClient
            .GetAllProductsAsync(Arg.Any<CancellationToken>())
            .Returns([]);

        var response = await _client.GetAsync("/products");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var body = await response.Content.ReadFromJsonAsync<List<ProductResponse>>();
        Assert.That(body, Is.Empty);
    }

    [Test]
    public async Task Returns200_WithProducts()
    {
        var products = new List<ProductResponse>
        {
            new(Guid.NewGuid(), "Laptop", "High-end laptop", 999.99m, 10),
            new(Guid.NewGuid(), "Mouse",  "Wireless mouse",   29.99m, 50)
        };
        _factory.CatalogClient
            .GetAllProductsAsync(Arg.Any<CancellationToken>())
            .Returns(products);

        var response = await _client.GetAsync("/products");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var body = await response.Content.ReadFromJsonAsync<List<ProductResponse>>();
        Assert.That(body, Has.Count.EqualTo(2));
    }

    [Test]
    public async Task Returns500_WhenClientThrows()
    {
        _factory.CatalogClient
            .GetAllProductsAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromException<IEnumerable<ProductResponse>>(
                new HttpRequestException("CatalogService unavailable")));

        var response = await _client.GetAsync("/products");

        Assert.That((int)response.StatusCode, Is.EqualTo(500));
    }
}
