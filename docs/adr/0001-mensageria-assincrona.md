# ADR 0001 — Comunicação assíncrona via mensageria (RabbitMQ)

**Status:** Aceito

## Contexto
O requisito não-funcional central do desafio: *"o serviço de controle de lançamento não deve
ficar indisponível se o sistema de consolidado diário cair"*. Uma chamada **síncrona** de
Lançamentos → Consolidado acoplaria a disponibilidade dos dois: se o Consolidado caísse (ou
ficasse lento no pico de 50 req/s), o Lançamentos falharia ou degradaria junto.

## Decisão
Os serviços se comunicam de forma **assíncrona** por **eventos de integração** em um broker de
mensagens (**RabbitMQ**). O Lançamentos publica `LancamentoRegistradoEvent`; o Consolidado
consome e projeta o saldo. Não há chamada HTTP direta entre eles.

Topologia: **exchange topic durável** + **fila durável** no consumidor + **dead-letter queue**
para mensagens com falha persistente.

## Alternativas consideradas
- **Chamada HTTP síncrona:** simples, mas viola o NFR de disponibilidade. Rejeitada.
- **Apache Kafka:** excelente para alto throughput/event sourcing, porém mais pesado para o
  cenário (50 req/s) e para rodar localmente. Bom candidato futuro (ver [`future.md`](../future.md)).
- **Azure Service Bus:** ótimo gerenciado em produção, mas não roda 100% local sem emulador.

## Consequências
- ✅ Lançamentos permanece disponível mesmo com o Consolidado fora; mensagens acumulam e são
  processadas no catch-up.
- ✅ Absorve picos (a fila funciona como buffer).
- ⚠️ **Consistência eventual**: o saldo reflete os lançamentos com pequeno atraso.
- ⚠️ Exige garantir entrega confiável (ver [ADR 0003 — Outbox](0003-padrao-outbox.md)) e
  consumo idempotente (reentrega *at-least-once*).
