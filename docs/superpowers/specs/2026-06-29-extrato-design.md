# Spec — Extrato de lançamentos do dia

**Data:** 2026-06-29
**Status:** Implementado

> **Atualização pós-implementação:** o painel "Extrato" embutido no console descrito nesta spec
> foi posteriormente **substituído por uma tela dedicada** (`/extrato`, ver a spec
> `2026-06-29-tela-extrato-design.md`) e removido do console (commit `f9cac0b`) para eliminar a
> duplicação. O **backend** do extrato (query + endpoint) permanece como descrito aqui.

## Objetivo
Permitir visualizar o **extrato** (lista dos lançamentos de débito/crédito de um comerciante
em um dia), complementando o saldo consolidado (que mostra apenas os totais agregados).

## Decisão arquitetural
O extrato é servido pela **API de Lançamentos**, que é a dona dos lançamentos individuais.
O serviço de Consolidado guarda só a projeção agregada (`SaldoDiario`), não os itens. Servir o
extrato pelo Lançamentos evita duplicar dados e usa o índice já existente `(comerciante, data)`.

Contraste documentado (reforça CQRS / separação de responsabilidades):
- **Extrato** (detalhe transacional, baixa frequência) → serviço de **Lançamentos**.
- **Saldo consolidado** (agregado, caminho de 50 req/s) → serviço de **Consolidado** (read-side).

## Backend (serviço de Lançamentos)
- **Query**: `ObterExtratoQuery(comercianteId, data)` → `Result<ExtratoDto>`.
- **DTO** `ExtratoDto`:
  - `comercianteId`, `data`
  - `itens`: lista de `LancamentoDto` (id, valor, tipo, data, criadoEmUtc, descrição)
  - `totalCreditos`, `totalDebitos`, `saldo`, `quantidade` (calculados a partir dos itens)
- **Repositório**: `ILancamentoRepository.GetPorComercianteEDataAsync(comercianteId, data)`
  → `IReadOnlyList<Lancamento>` (ordenado por `CriadoEmUtc`), usando o índice existente.
- **Handler**: monta os itens, calcula os totais; **dia sem lançamentos → extrato vazio**
  (lista vazia + zeros), não 404.
- **Endpoint**: `GET /lancamentos/extrato/{comercianteId}/{data}` (autenticado). A rota não
  conflita com `GET /lancamentos/{id:guid}` (segmentos e constraint distintos).

## Frontend (novo painel "Extrato")
- Componente `Extrato.vue`, full-width, abaixo do consolidado.
- Campo de data (padrão hoje) + botão "Ver extrato".
- Lista os itens: tipo (crédito/débito colorido), valor, hora (`criadoEmUtc`), descrição.
- Rodapé de resumo: créditos, débitos, saldo, quantidade.
- Estado vazio amigável quando não há lançamentos no dia.
- `api.ts`: `obterExtrato(token, comerciante, data)` + tipos `ExtratoDto`/item.

## Testes
- Unitário de `ObterExtratoQueryHandler`:
  - retorna itens e totais corretos (créditos − débitos);
  - dia sem lançamentos → extrato vazio com zeros.

## Documentação
- ADR 0005 (CQRS): nota sobre extrato (Lançamentos) × consolidado (Consolidado).
- README: novo endpoint na tabela e exemplo de uso.

## Fora de escopo (YAGNI)
- Paginação e filtros por período (só por dia, alinhado ao tema "consolidado diário").
- Exportação (CSV/PDF) — possível evolução futura.

## Critérios de sucesso
- `GET /lancamentos/extrato/{comerciante}/{hoje}` retorna os itens do dia + totais.
- O painel "Extrato" no console (porta 8080) lista os lançamentos com o resumo.
