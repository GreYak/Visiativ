using BasketService.Filters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Web.Http;

namespace BasketService
{
    public static class WebApiConfig
    {
        public static void Register(HttpConfiguration config)
        {
            // Filtre d'exception global : log + JSON 500 uniforme pour toute exception non gérée
            config.Filters.Add(new GlobalExceptionFilter());

            // Configuration et services de l'API Web

            // Itinéraires de l'API Web
            config.MapHttpAttributeRoutes();

            config.Routes.MapHttpRoute(
                name: "DefaultApi",
                routeTemplate: "api/{controller}/{id}",
                defaults: new { id = RouteParameter.Optional }
            );

            config.Formatters.Remove(config.Formatters.XmlFormatter);
            config.Formatters.JsonFormatter.SupportedMediaTypes
                .Add(new MediaTypeHeaderValue("application/json"));

            config.EnsureInitialized();
        }
    }
}
