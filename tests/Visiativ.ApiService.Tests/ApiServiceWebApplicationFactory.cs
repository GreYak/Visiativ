using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NSubstitute;
using Visiativ.ApiService.Abstractions;

namespace Visiativ.ApiService.Tests;

public class ApiServiceWebApplicationFactory : WebApplicationFactory<Program>
{
    public ICatalogClient CatalogClient { get; } = Substitute.For<ICatalogClient>();
    public IBasketClient  BasketClient  { get; } = Substitute.For<IBasketClient>();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<ICatalogClient>();
            services.RemoveAll<IBasketClient>();
            services.AddSingleton(CatalogClient);
            services.AddSingleton(BasketClient);
        });
    }
}
