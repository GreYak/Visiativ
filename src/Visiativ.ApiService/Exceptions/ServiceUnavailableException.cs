namespace Visiativ.ApiService.Exceptions;

/// <summary>Levée par un client HTTP quand le service distant est injoignable ou retourne une erreur 5xx.</summary>
public sealed class ServiceUnavailableException(string serviceName)
    : Exception($"Le service '{serviceName}' est temporairement indisponible.")
{
    public string ServiceName { get; } = serviceName;
}
