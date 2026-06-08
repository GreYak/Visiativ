namespace Visiativ.ApiService.Exceptions;

/// <summary>Levée par un client HTTP quand le service distant retourne une erreur 409 (conflit d'état).</summary>
public sealed class RemoteConflictException(string message) : Exception(message);
