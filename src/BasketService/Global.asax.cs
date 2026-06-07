using Autofac;
using Autofac.Integration.WebApi;
using BasketService.Domain;
using BasketService.Domain.Ports.Spi;
using BasketService.Infrastructure;
using System.Reflection;
using System.Web.Http;

namespace BasketService
{
    public class WebApiApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            var config = GlobalConfiguration.Configuration;
            WebApiConfig.Register(config);

            // Autofac
            var builder = new ContainerBuilder();
            builder.RegisterApiControllers(Assembly.GetExecutingAssembly());
            builder.RegisterType<BasketItemRepository>().As<IBasketItemRepository>().InstancePerRequest();
            builder.RegisterType<GetBasket>().InstancePerRequest();
            builder.RegisterType<AddItemToBasket>().InstancePerRequest();
            builder.RegisterType<DeleteBasket>().InstancePerRequest();
            var container = builder.Build();
            config.DependencyResolver = new AutofacWebApiDependencyResolver(container);

            DatabaseInitializer.Initialize();
        }
    }
}
