using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Visiativ.ServiceDefaults.Middlewares;

/// <summary>
/// Middleware partagé catch-all : log l'exception et retourne une réponse JSON 500 uniforme.
/// À enregistrer en position la plus externe du pipeline (avant tout autre middleware).
/// Les erreurs métier (4xx, 503…) doivent être gérées en amont, par les endpoints eux-mêmes.
/// </summary>
public sealed class ExceptionHandlingMiddleware(
    RequestDelegate next,
    ILogger<ExceptionHandlingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "Exception non gérée sur {Method} {Path}",
                context.Request.Method,
                context.Request.Path);

            if (context.Response.HasStarted)
                throw;

            context.Response.StatusCode  = StatusCodes.Status500InternalServerError;
            context.Response.ContentType = "application/json";

            await context.Response.WriteAsJsonAsync(new
            {
                status = 500,
                error  = "Une erreur inattendue s'est produite."
            });
        }
    }
}
