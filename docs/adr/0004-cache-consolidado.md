# ADR 0004 — Projeção pré-calculada + cache no Consolidado

**Status:** Aceito

## Contexto
Requisito não-funcional: *"em dias de pico, o serviço de consolidado diário recebe 50
requisições por segundo, com no máximo 5% de perda de requisições"*. Calcular o saldo somando
todos os lançamentos a cada requisição não escala (custo cresce com o histórico).

## Decisão
O Consolidado mantém uma **projeção pré-calculada** (`SaldoDiario`), atualizada
incrementalmente a cada evento. A leitura é servida por **cache distribuído (Redis)** em
estratégia **read-through**; em miss, busca a projeção (O(1)) e popula o cache. A escrita
**invalida** a entrada do dia.

Para o pico, a `Consolidado.API` é **stateless** (escala horizontal) e tem **rate limiting**
(load shedding): requisições acima do limite recebem `429` imediatamente — descarte controlado
que respeita a tolerância de 5% em vez de degradar todo o serviço.

## Alternativas consideradas
- **Somar lançamentos on-the-fly:** custo crescente, não sustenta 50 req/s. Rejeitada.
- **Cache sem invalidação (só TTL):** simples, mas serve dado velho por mais tempo. Adotamos
  invalidação na escrita + TTL curto como rede de segurança.

## Consequências
- ✅ Leitura O(1) com baixa latência; sustenta o pico com folga.
- ✅ Resiliência: falha no Redis **degrada para o banco** (o cache é acelerador, não SPOF).
- ⚠️ Cache e banco precisam ser mantidos coerentes (invalidação na escrita).
- ⚠️ Janela mínima de inconsistência entre a projeção e o cache (consistência eventual).
