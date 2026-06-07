using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Visiativ.ServiceDefaults.Networking;

/// <summary>
/// DelegatingHandler de log des requêtes HTTP sortantes.
/// S'intercale dans la pipeline HttpClient et s'applique à tous les clients
/// enregistrés via ConfigureHttpClientDefaults.
///
/// - LogInformation : requête sortante (méthode + URI + body JSON pour POST/PUT/PATCH) et réponse 2xx/3xx
/// - LogWarning     : réponse 4xx ou 5xx (le downstream a répondu mais en erreur)
/// - LogError       : exception réseau (timeout, connexion refusée, DNS…)
///
/// Le body est tronqué à <see cref="MaxBodyLogLength"/> caractères pour éviter de saturer les logs.
/// ⚠️ Ne pas activer sur des endpoints transmettant des données sensibles (mots de passe, tokens…)
///    sans filtrage préalable.
/// </summary>
public sealed class OutboundHttpLoggingHandler(ILogger<OutboundHttpLoggingHandler> logger) : DelegatingHandler
{
    /// <summary>Taille maximale du body loggué (caractères). Au-delà, le contenu est tronqué.</summary>
    private const int MaxBodyLogLength = 2000;

    private static readonly HttpMethod[] _bodyMethods =
        [HttpMethod.Post, HttpMethod.Put, HttpMethod.Patch];

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var method = request.Method.Method;
        var uri    = request.RequestUri?.ToString() ?? "(unknown)";
        var body   = await TryReadBodyAsync(request, cancellationToken);

        if (body is not null)
            logger.LogInformation("→ [OUT] {Method} {Uri} body={Body}", method, uri, body);
        else
            logger.LogInformation("→ [OUT] {Method} {Uri}", method, uri);

        var sw = Stopwatch.StartNew();

        HttpResponseMessage response;
        try
        {
            response = await base.SendAsync(request, cancellationToken);
        }
        catch (Exception ex)
        {
            sw.Stop();
            logger.LogError(ex,
                "← [OUT] {Method} {Uri} — exception après {ElapsedMs}ms",
                method, uri, sw.ElapsedMilliseconds);
            throw;
        }

        sw.Stop();

        var status  = (int)response.StatusCode;
        var elapsed = sw.ElapsedMilliseconds;

        if (status >= 400)
            logger.LogWarning("← [OUT] {Method} {Uri} responded {StatusCode} in {ElapsedMs}ms",
                method, uri, status, elapsed);
        else
            logger.LogInformation("← [OUT] {Method} {Uri} responded {StatusCode} in {ElapsedMs}ms",
                method, uri, status, elapsed);

        return response;
    }

    /// <summary>
    /// Lit le body pour les méthodes POST/PUT/PATCH uniquement.
    /// Utilise <see cref="HttpContent.LoadIntoBufferAsync"/> pour bufferiser le stream
    /// avant lecture — le contenu reste disponible pour l'envoi HTTP effectif.
    /// </summary>
    private static async Task<string?> TryReadBodyAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        if (request.Content is null || !_bodyMethods.Contains(request.Method))
            return null;

        // Bufferise le stream en mémoire — indispensable pour pouvoir le lire
        // sans le consommer : le HttpClient pourra toujours envoyer le body ensuite.
        await request.Content.LoadIntoBufferAsync(cancellationToken);

        var raw = await request.Content.ReadAsStringAsync(cancellationToken);

        return raw.Length > MaxBodyLogLength
            ? raw[..MaxBodyLogLength] + $"… [tronqué à {MaxBodyLogLength} car.]"
            : raw;
    }
}
