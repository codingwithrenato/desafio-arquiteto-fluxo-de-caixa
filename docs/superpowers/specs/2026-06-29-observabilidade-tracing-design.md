# Spec — Observabilidade: tracing distribuído (OpenTelemetry → Jaeger)

**Data:** 2026-06-29
**Status:** Aprovado

## Objetivo
Rastrear, em **um único trace distribuído**, o fluxo assíncrono completo do sistema:
`POST /lancamentos` → escrita no Outbox → publish do dispatcher → RabbitMQ → consumer →
consolidação → projeção (com os spans de banco). Visualização no **Jaeger**.

## Princípio: OpenTelemetry (vendor-neutral)
A instrumentação é feita com **OpenTelemetry**; o backend é plugável via exporter OTLP.
Em dev → Jaeger (1 container, grátis). Em produção → Datadog/New Relic/Grafana Tempo/Azure
Monitor trocando apenas o endpoint/exporter por configuração — **sem mudança de código**.

## Instrumentação
- **SharedKernel**: `ObservabilityExtensions.AddObservability(services, configuration, serviceName)`
  configura OTel Tracing:
  - Instrumentação ASP.NET Core (HTTP de entrada) e HttpClient.
  - Fonte do Npgsql (spans de banco) e a ActivitySource da aplicação.
  - Resource com `service.name`.
  - Exporter OTLP (endpoint via `OTEL_EXPORTER_OTLP_ENDPOINT`; ausente ⇒ tracing no-op).
- Os 3 hosts (Lancamentos.API, Consolidado.API, Consolidado.Worker) chamam `AddObservability`.

## Propagação pelo RabbitMQ (tracing através do Outbox)
Para conectar o POST original à consolidação (que ocorre depois, em outro processo):
1. **Enqueue** (escopo do POST): captura o `traceparent` (W3C) do `Activity.Current` e grava na
   **nova coluna `TraceParent`** da tabela `outbox_messages`.
2. **Dispatcher**: abre um span *producer* parentado no `TraceParent` armazenado e injeta o
   contexto W3C nos headers da mensagem RabbitMQ (via propagator OTel).
3. **Consumer**: extrai o contexto dos headers e abre o span *consumer* como filho — costurando
   o trace ponta a ponta.

Helper de propagação (`inject`/`extract`) e a constante da ActivitySource ficam em
**BuildingBlocks** (compartilhados por publisher e consumer), usando `OpenTelemetry.Api`.

### Migration
Adiciona a coluna `trace_parent` (nullable) em `outbox_messages` (banco de Lançamentos).

## Infra
- **1 container** `jaegertracing/all-in-one` (UI em 16686, OTLP em 4317), storage em memória.
- `docker-compose`: nos 3 serviços, `OTEL_EXPORTER_OTLP_ENDPOINT=http://jaeger:4317` e
  `OTEL_SERVICE_NAME` por serviço.

## Pacotes
- SharedKernel: `OpenTelemetry.Extensions.Hosting`, `OpenTelemetry.Instrumentation.AspNetCore`,
  `OpenTelemetry.Instrumentation.Http`, `OpenTelemetry.Exporter.OpenTelemetryProtocol`.
- BuildingBlocks: `OpenTelemetry.Api`.

## Verificação
- Manual/visual: subir a stack, `POST /lancamentos`, abrir o Jaeger (16686) e confirmar o trace
  conectado (POST → outbox.publish → consume → spans de banco), atravessando os serviços.
- Build limpo e os 25 testes existentes seguem verdes (instrumentação cross-cutting não adiciona
  teste unitário).

## Fora de escopo (YAGNI)
- Métricas (Prometheus) e dashboards (Grafana) — documentados como próxima camada (peso de
  memória no Docker local).
- Logs correlacionados via OTel logs (Serilog já cobre logging estruturado; correlação por
  trace id fica como evolução).

## Docs
- ADR 0007 (observabilidade com OTel + propagação de contexto no Outbox).
- Atualizar `architecture.md`, `non-functional.md`; mover o item correspondente em `future.md`.

## Critérios de sucesso
- Um POST gera, no Jaeger, um trace único que vai da requisição HTTP até a consolidação no
  Worker, com os spans de banco — provando o rastreamento através da fronteira assíncrona.
