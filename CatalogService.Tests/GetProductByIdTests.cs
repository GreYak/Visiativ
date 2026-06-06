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
public class GetProductByIdTests
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
    public async Task GetById_ReturnsOk_WhenProductExists()
    {
        var product = Product.Create("Keyboard", "Mechanical keyboard", 89.99m, 25);
        await _factory.SeedAsync(db => db.Products.Add(product));

        var response = await _client.GetAsync($"/products/{product.Id}");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var body = await response.Content.ReadFromJsonAsync<ProductResponse>();
        Assert.That(body!.Name, Is.EqualTo("Keyboard"));
    }


    [Test]
    public async Task GetById_ReturnsNotFound_WhenProductDoesNotExist()
    {
        var response = await _client.GetAsync($"/products/{Guid.NewGuid()}");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    [Test]
    public async Task GetById_Returns500_WhenDbThrows()
    {
        var options = new DbContextOptionsBuilder<CatalogDbContext>()
            .UseInMemoryDatabase("getbyid-exception")
            .Options;

        var fakeDb = Substitute.ForPartsOf<CatalogDbContext>(options);
        fakeDb.When(ctx => ctx.Set<Product>())
              .Throw<InvalidOperationException>();

        using var factory = new CatalogWebApplicationFactory(
            services => services.AddScoped<CatalogDbContext>(_ => fakeDb));
        using var client = factory.CreateClient();

        var response = await client.GetAsync($"/products/{Guid.NewGuid()}");

        Assert.That((int)response.StatusCode, Is.EqualTo(500));
    }
}
