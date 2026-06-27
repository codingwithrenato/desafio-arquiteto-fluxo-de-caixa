namespace BuildingBlocks.Contracts;

/// <summary>
/// Tipo de lançamento no fluxo de caixa. Faz parte do contrato de integração
/// (shared kernel), por isso vive em BuildingBlocks e não no domínio de um serviço.
/// </summary>
public enum TipoLancamento
{
    Credito = 1,
    Debito = 2
}
