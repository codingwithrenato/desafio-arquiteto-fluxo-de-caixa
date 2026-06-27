namespace SharedKernel.Security;

/// <summary>Configuração de emissão/validação de tokens JWT (Options pattern).</summary>
public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = "fluxo-caixa";
    public string Audience { get; set; } = "fluxo-caixa-clients";

    /// <summary>Chave simétrica HMAC. Em produção vem de cofre (Key Vault/Secrets), nunca do código.</summary>
    public string SigningKey { get; set; } = default!;

    public int ExpiryMinutes { get; set; } = 60;
}
