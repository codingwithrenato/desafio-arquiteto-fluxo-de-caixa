# ADR 0007 — Observabilidade com OpenTelemetry (tracing distribuído)

**Status:** Aceito

## Contexto
Num sistema de microsserviços com comunicação **assíncrona** (Outbox → RabbitMQ → consumer),
diagnosticar um problema ("o saldo não atualizou") é difícil: a causa pode estar em qualquer
ponto de uma cadeia que cruza processos e o broker. Logs isolados por serviço não mostram o
caminho ponta a ponta de uma requisição.

## Decisão
Instrumentar os serviços com **OpenTelemetry** (padrão vendor-neutral) para **tracing
distribuído**, exportando via **OTLP**. Em desenvolvimento o backend é o **Jaeger** (1 container);
em produção, troca-se o exporter para Datadog/New Relic/Grafana Tempo/Azure Monitor **apenas por
configuração** (`OTEL_EXPORTER_OTLP_ENDPOINT`), sem mudança de código — evitando vendor lock-in.

Instrumentação (em `SharedKernel.AddObservability`): ASP.NET Core, HttpClient, Npgsql (spans de
banco) e a ActivitySource da aplicação.

### Propagação de contexto através do Outbox (o ponto central)
Para conectar a requisição original (POST) à consolidação (que ocorre depois, em outro processo):
1. No **enqueue**, grava-se o `traceparent` (W3C) da requisição na coluna `trace_parent` da
   tabela de outbox.
2. No **dispatcher**, abre-se um span *producer* parentado nesse `traceparent` e injeta-se o
   contexto W3C nos **headers da mensagem** RabbitMQ.
3. No **consumer**, extrai-se o contexto dos headers e abre-se o span *consumer* como filho.

Resultado: um único trace cobre `POST → outbox.publish → consume → projeção`, atravessando dois
serviços e o broker (verificado no Jaeger: 1 trace, 2 serviços, spans encadeados).

## Alternativas consideradas
- **Stack completo agora (Prometheus + Grafana + Collector):** métricas e dashboards além de
  traces. Adiaria por peso de recursos no ambiente local; fica como evolução (ver `future.md`).
- **APM proprietário (Datadog/New Relic) direto:** ótimo em produção, mas pago e com lock-in.
  Com OTel, ele continua sendo uma opção — plugável por configuração.
- **Só correlacionar logs por requestId:** ajuda, mas não dá a visão de cascata (waterfall) nem
  os tempos por etapa que o tracing oferece.

## Logs: correlação e span events
O Jaeger é um backend de **traces**, não um agregador de logs. Para unir os pilares sem um
backend de logs dedicado:
- **Correlação por trace_id:** o Serilog é enriquecido com `TraceId`/`SpanId` da Activity atual
  (`ActivityEnricher`), então cada linha de log carrega o `trace_id`. Pega-se um trace no Jaeger
  e encontram-se os logs daquele fluxo — inclusive **cruzando serviços** (o mesmo `trace_id`
  aparece nos logs do Lançamentos e do Consolidado).
- **Span events:** anotações de domínio nos pontos-chave (`lançamento registrado`,
  `idempotencia: evento já processado`, `consolidado atualizado`) aparecem **dentro do span** no
  Jaeger, com tags — visíveis na própria linha do tempo do trace.

Visualização de logs em UI unificada (Loki/Grafana ou ELK) fica como evolução (ver `future.md`).

## Consequências
- ✅ Diagnóstico ponta a ponta do fluxo assíncrono, com latência por etapa.
- ✅ Logs correlacionados ao trace (mesmo `trace_id`) e eventos de domínio visíveis no trace.
- ✅ Sem lock-in: mesma instrumentação serve qualquer backend OTLP.
- ✅ Overhead baixo; sem `OTEL_EXPORTER_OTLP_ENDPOINT`, o tracing é praticamente no-op.
- ⚠️ Propagar o contexto pelo Outbox exigiu uma coluna extra (`trace_parent`) e cuidado para
  parentar o span do dispatcher no trace original.
- ⚠️ Métricas (Prometheus) e UI unificada de logs ainda não cobertas (próxima camada).
