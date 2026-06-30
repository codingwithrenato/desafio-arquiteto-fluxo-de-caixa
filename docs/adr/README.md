# Architecture Decision Records (ADRs)

Registro das principais decisões de arquitetura da solução, com contexto, alternativas e
consequências. O formato segue o padrão de ADR (Michael Nygard).

| # | Decisão | Resumo |
|---|---|---|
| [0001](0001-mensageria-assincrona.md) | Mensageria assíncrona (RabbitMQ) | Desacopla Lançamentos do Consolidado — NFR central |
| [0002](0002-database-per-service.md) | Database-per-service | Isolamento de dados e de falhas |
| [0003](0003-padrao-outbox.md) | Padrão Transactional Outbox | Entrega confiável de eventos (sem dual-write) |
| [0004](0004-cache-consolidado.md) | Projeção + cache no Consolidado | Sustenta 50 req/s na leitura |
| [0005](0005-cqrs.md) | CQRS | Separa escrita de leitura |
| [0006](0006-hangfire-worker.md) | Hangfire isolado no Worker | Jobs sem impactar a escala de leitura |
| [0007](0007-observabilidade-opentelemetry.md) | Observabilidade com OpenTelemetry | Tracing distribuído ponta a ponta (vendor-neutral) |
