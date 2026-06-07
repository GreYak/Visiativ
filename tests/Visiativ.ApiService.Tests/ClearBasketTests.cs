using NSubstitute;
using NSubstitute.ClearExtensions;
using NUnit.Framework;
using System.Net;
using Visiativ.ApiService.Exceptions;

namespace Visiativ.ApiService.Tests;

/// <summary>DELETE /basket</summary>
[TestFixture]
public class ClearBasketTests
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
    public async Task Returns204_OnSuccess()
    {
        _factory.BasketClient
            .ClearBasketAsync(Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var response = await _client.DeleteAsync("/basket");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
    }

    [Test]
    public async Task Returns503_WhenClientUnavailable()
    {
        _factory.BasketClient
            .ClearBasketAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromException(new ServiceUnavailableException("BasketService")));

        var response = await _client.DeleteAsync("/basket");

        Assert.That((int)response.StatusCode, Is.EqualTo(503));
    }
}
