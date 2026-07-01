# ADR 0004 — Projeção pré-calculada + cache no Consolidado

**Status:** Aceito

## Contexto
Requisito não-funcional: *"em dias de pico, o serviço de consolidado diário recebe 50
requisições por segundo, com no máximo 5% de perda de requisições"*. Calcular o saldo somando
todos os lançamentos a cada requisição não escala (custo cresce com o histórico).

## Decisão
O Consolidado mantém uma **projeção pré-calculada** (`SaldoDiario`), atualizada
incrementalmente a cada evento. A leitura é servida por **cache distribuído (Redis)**:
- **Escrita (consolidação):** estratégia **write-through** — a cada evento, após persistir a
  projeção, grava no cache o valor autoritativo recém-calculado.
- **Leitura:** lê do cache; em miss, busca a projeção (O(1)) e popula o cache.
- **TTL curto (60s):** rede de segurança que limita qualquer leitura desatualizada.

**Por que write-through e não simples invalidação:** a invalidação tem uma race conhecida —
um leitor lento que leu o banco antes da escrita pode repovoar o cache com o valor antigo
*depois* da invalidação, deixando-o velho até o TTL. Com write-through, cada evento reafirma o
valor correto, e o TTL curto limita o resíduo a poucos segundos (consistente com a natureza
eventualmente consistente do sistema).

Para o pico, a `Consolidado.API` é **stateless** (escala horizontal) e tem **rate limiting**
(load shedding): requisições acima do limite recebem `429` imediatamente — descarte controlado
que respeita a tolerância de 5% em vez de degradar todo o serviço.

## Alternativas consideradas
- **Somar lançamentos on-the-fly:** custo crescente, não sustenta 50 req/s. Rejeitada.
- **Cache sem atualização na escrita (só TTL):** simples, mas serve dado velho por mais tempo.
  Adotamos **write-through** na escrita (grava o saldo autoritativo recém-calculado) + TTL curto
  como rede de segurança.
- **Invalidação na escrita (remover a chave):** exige releitura no próximo GET e abre janela de
  corrida (um leitor lento pode repovoar valor velho após a invalidação). Preferimos write-through.

## Consequências
- ✅ Leitura O(1) com baixa latência; sustenta o pico com folga.
- ✅ Resiliência: falha no Redis **degrada para o banco** (o cache é acelerador, não SPOF).
- ✅ Write-through mantém o cache quente e correto durante períodos de atividade.
- ⚠️ Janela mínima de inconsistência (≤ TTL) entre a projeção e o cache (consistência eventual).
- ⚠️ Correção estrita sob a race exigiria *compare-and-set*/versionamento (ex.: Redis `SETNX`
  ou Lua com versão `atualizadoEmUtc`) — registrado como evolução futura.
