using System;
using System.Net;
using System.Net.Http;
using System.Web.Http.Filters;

namespace BasketService.Filters
{
    /// <summary>
    /// Filtre global ASP.NET WebAPI : intercepte toutes les exceptions non gérées,
    /// les journalise et retourne une réponse JSON 500 uniforme.
    /// Enregistré via <see cref="BasketService.App_Start.WebApiConfig"/>.
    /// </summary>
    public sealed class GlobalExceptionFilter : ExceptionFilterAttribute
    {
        public override void OnException(HttpActionExecutedContext context)
        {
            Console.Error.WriteLine(
                $"[BasketService] {context.Request.Method} {context.Request.RequestUri} => {context.Exception}");

            System.Diagnostics.Trace.TraceError(
                "[BasketService] Exception non gérée sur {0} {1} : {2}",
                context.Request.Method,
                context.Request.RequestUri,
                context.Exception);

            context.Response = context.Request.CreateResponse(
                HttpStatusCode.InternalServerError,
                new
                {
                    status = 500,
                    error  = "Une erreur inattendue s'est produite."
                });
        }
    }
}
