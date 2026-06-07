using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace BasketService.Handlers
{
    /// <summary>
    /// DelegatingHandler : log chaque requête entrante et chaque réponse sortante.
    /// En cas d'erreur (>= 400) le body de la réponse est également loggué.
    /// </summary>
    public sealed class LoggingHandler : DelegatingHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            Console.WriteLine($"[BasketService] --> {request.Method} {request.RequestUri.PathAndQuery}");

            HttpResponseMessage response;
            try
            {
                response = await base.SendAsync(request, cancellationToken);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[BasketService] !! Exception non interceptée : {ex}");
                throw;
            }

            int status = (int)response.StatusCode;

            if (status >= 400)
            {
                string body = string.Empty;
                if (response.Content != null)
                    body = await response.Content.ReadAsStringAsync();

                Console.Error.WriteLine($"[BasketService] <-- {status} {response.StatusCode} | body: {body}");
            }
            else
            {
                Console.WriteLine($"[BasketService] <-- {status} {response.StatusCode}");
            }

            return response;
        }
    }
}
