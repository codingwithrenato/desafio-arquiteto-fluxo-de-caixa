namespace Lancamentos.Domain;

/// <summary>Violação de uma invariante de domínio.</summary>
public sealed class DomainException(string message) : Exception(message);
