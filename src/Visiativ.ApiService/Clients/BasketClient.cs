using System.Net;
using Visiativ.ApiService.Abstractions;
using Visiativ.ApiService.Exceptions;
using Visiativ.ApiService.Models;

namespace Visiativ.ApiService.Clients;

public class BasketClient(HttpClient http) : IBasketClient
{
    public async Task<IEnumerable<BasketItem>> GetBasketAsync(CancellationToken ct = default)
    {
        try
        {
            var result = await http.GetFromJsonAsync<IEnumerable<BasketItem>>("/api/basket", ct);
            return result ?? [];
        }
        catch (HttpRequestException)
        {
            throw new ServiceUnavailableException("BasketService");
        }
    }

    public async Task AddItemAsync(BasketItem item, int? limitMax = null, CancellationToken ct = default)
    {
        HttpResponseMessage response;
        try
        {
            // PostAsJsonAsync utilise JsonContent dont la longueur est parfois inconnue
            // au moment de l'envoi → HttpClient peut envoyer en chunked transfer encoding.
            // XSP4/Mono 6.12 ne lit pas correctement un body chunked de façon async
            // → Socket.Receive(non-bloquant) → WOULDBLOCK.
            // Fix : StringContent + ContentLength explicite → body envoyé en une seule fois
            // avec header Content-Length (pas de chunked), + désactivation Expect: 100-continue.
            var body = new
            {
                productId = item.ProductId,
                quantity  = item.Quantity,
                limitMax
            };
            var json  = System.Text.Json.JsonSerializer.Serialize(body);
            var bytes = System.Text.Encoding.UTF8.GetBytes(json);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
            content.Headers.ContentLength = bytes.Length;

            var req = new HttpRequestMessage(HttpMethod.Post, "/api/basket/add")
            {
                Content = content
            };
            // Désactive Expect: 100-continue — évite que XSP4 doive envoyer 100 avant le body
            req.Headers.ExpectContinue = false;

            response = await http.SendAsync(req, ct);
        }
        catch (HttpRequestException)
        {
            throw new ServiceUnavailableException("BasketService");
        }

        if (response.StatusCode == HttpStatusCode.BadRequest)
        {
            var message = await response.Content.ReadAsStringAsync(ct);
            throw new RemoteValidationException(message);
        }

        if (response.StatusCode == HttpStatusCode.Conflict)
        {
            var message = await response.Content.ReadAsStringAsync(ct);
            throw new RemoteConflictException(message);
        }

        if (!response.IsSuccessStatusCode)
            throw new ServiceUnavailableException("BasketService");
    }

    public async Task ClearBasketAsync(CancellationToken ct = default)
    {
        HttpResponseMessage response;
        try
        {
            response = await http.DeleteAsync("/api/basket", ct);
        }
        catch (HttpRequestException)
        {
            throw new ServiceUnavailableException("BasketService");
        }

        if (!response.IsSuccessStatusCode)
            throw new ServiceUnavailableException("BasketService");
    }
}
