using System.Net;
using System.Net.Http.Json;
using CatalogService.Domain;
using CatalogService.Infrastructure.Api;
using CatalogService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using NUnit.Framework;

namespace CatalogService.Tests;

[TestFixture]
public class GetAllProductsTests
{
    private CatalogWebApplicationFactory _factory = null!;
    private HttpClient _client = null!;

    [SetUp]
    public void SetUp()
    {
        _factory = new CatalogWebApplicationFactory();
        _client  = _factory.CreateClient();
    }

    [TearDown]
    public void TearDown()
    {
        _client.Dispose();
        _factory.Dispose();
    }

    [Test]
    public async Task GetAll_ReturnsOk_WithEmptyList()
    {
        var response = await _client.GetAsync("/products");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var body = await response.Content.ReadFromJsonAsync<List<ProductResponse>>();
        Assert.That(body, Is.Empty);
    }

    [Test]
    public async Task GetAll_ReturnsOk_WithProducts()
    {
        await _factory.SeedAsync(db =>
        {
            db.Products.Add(Product.Create("Laptop",  "High-end laptop",  999.99m, 10));
            db.Products.Add(Product.Create("Mouse",   "Wireless mouse",    29.99m, 50));
        });

        var response = await _client.GetAsync("/products");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var body = await response.Content.ReadFromJsonAsync<List<ProductResponse>>();
        Assert.That(body, Has.Count.EqualTo(2));
    }

    [Test]
    public async Task GetAll_Returns500_WhenDbThrows()
    {
        var options = new DbContextOptionsBuilder<CatalogDbContext>()
            .UseInMemoryDatabase("getall-exception")
            .Options;
        var fakeDb = Substitute.ForPartsOf<CatalogDbContext>(options);
        fakeDb.When(ctx => ctx.Set<Product>())
              .Throw<InvalidOperationException>();

        using var factory = new CatalogWebApplicationFactory(
            services => services.AddScoped<CatalogDbContext>(_ => fakeDb));
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/products");

        Assert.That((int)response.StatusCode, Is.EqualTo(500));
    }
}
