using Microsoft.AspNetCore.Diagnostics;
using Visiativ.ApiService.Exceptions;

namespace Visiativ.ApiService.ExceptionHandlers;

public sealed class RemoteValidationExceptionHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext context,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (exception is not RemoteValidationException ex)
            return false;

        context.Response.StatusCode = StatusCodes.Status400BadRequest;
        await context.Response.WriteAsJsonAsync(new
        {
            status = 400,
            error  = ex.Message
        }, cancellationToken);

        return true;
    }
}
