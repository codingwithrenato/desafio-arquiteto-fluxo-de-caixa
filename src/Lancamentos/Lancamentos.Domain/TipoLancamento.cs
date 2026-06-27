namespace Lancamentos.Domain;

/// <summary>
/// Tipo de lançamento no modelo de domínio. Propositalmente independente do enum
/// de contrato de integração (BuildingBlocks.Contracts.TipoLancamento): o domínio
/// não depende do contrato externo. A camada de Application faz o mapeamento.
/// </summary>
public enum TipoLancamento
{
    Credito = 1,
    Debito = 2
}
