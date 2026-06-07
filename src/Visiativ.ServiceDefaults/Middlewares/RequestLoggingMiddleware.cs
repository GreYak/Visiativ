using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Visiativ.ServiceDefaults.Middlewares;

/// <summary>
/// Middleware de log des requêtes HTTP entrantes.
/// - LogInformation : requête entrante (méthode + chemin + query string) et réponse 1xx/2xx/3xx
/// - LogWarning     : réponse 4xx (erreur client)
/// - LogError       : réponse 5xx (erreur serveur)
///
/// Sont silencieusement ignorés (pipeline traversé sans log) :
/// - health checks (/health, /alive)
/// - hub SignalR Blazor Server (/_blazor, /_framework)
/// - assets statiques (/_content, /lib/, extensions .css/.js/.woff…)
/// </summary>
public sealed class RequestLoggingMiddleware(
    RequestDelegate next,
    ILogger<RequestLoggingMiddleware> logger)
{
    // Préfixes de chemin exclus du log
    private static readonly string[] _excludedPrefixes =
    [
        "/health", "/alive",
        "/_blazor",     // SignalR hub Blazor Server
        "/_framework",  // Runtime Blazor WebAssembly
        "/_content",    // Assets Razor Class Libraries
        "/lib/",        // Assets statiques tiers (Bootstrap, jQuery…)
    ];

    // Extensions de fichier exclues du log
    private static readonly string[] _excludedExtensions =
    [
        ".css", ".js", ".mjs",
        ".woff", ".woff2", ".ttf", ".eot",
        ".png", ".jpg", ".jpeg", ".gif", ".svg", ".ico",
        ".map",
    ];

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value ?? string.Empty;

        if (IsExcluded(path))
        {
            await next(context);
            return;
        }

        var method   = context.Request.Method;
        var query    = context.Request.QueryString.HasValue ? context.Request.QueryString.Value : string.Empty;
        var fullPath = path + query;

        logger.LogInformation("→ {Method} {Path}", method, fullPath);

        var sw = Stopwatch.StartNew();
        await next(context);
        sw.Stop();

        var status  = context.Response.StatusCode;
        var elapsed = sw.ElapsedMilliseconds;

        if (status >= 500)
            logger.LogError("← {Method} {Path} responded {StatusCode} in {ElapsedMs}ms",
                method, fullPath, status, elapsed);
        else if (status >= 400)
            logger.LogWarning("← {Method} {Path} responded {StatusCode} in {ElapsedMs}ms",
                method, fullPath, status, elapsed);
        else
            logger.LogInformation("← {Method} {Path} responded {StatusCode} in {ElapsedMs}ms",
                method, fullPath, status, elapsed);
    }

    private static bool IsExcluded(string path) =>
        _excludedPrefixes.Any(p => path.StartsWith(p, StringComparison.OrdinalIgnoreCase))
        || _excludedExtensions.Any(e => path.EndsWith(e, StringComparison.OrdinalIgnoreCase));
}
