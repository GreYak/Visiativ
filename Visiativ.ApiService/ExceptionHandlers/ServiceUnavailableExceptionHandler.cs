using Microsoft.AspNetCore.Diagnostics;
using Visiativ.ApiService.Exceptions;

namespace Visiativ.ApiService.ExceptionHandlers;

public sealed class ServiceUnavailableExceptionHandler(ILogger<ServiceUnavailableExceptionHandler> logger)
    : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext context,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (exception is not ServiceUnavailableException ex)
            return false;

        logger.LogWarning("Service indisponible : {ServiceName}", ex.ServiceName);

        context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
        await context.Response.WriteAsJsonAsync(new
        {
            status = 503,
            error  = ex.Message
        }, cancellationToken);

        return true;
    }
}
