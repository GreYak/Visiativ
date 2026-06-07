namespace Visiativ.ApiService.Exceptions;

/// <summary>Levée par un client HTTP quand le service distant retourne une erreur 400 (validation métier).</summary>
public sealed class RemoteValidationException(string message) : Exception(message);
