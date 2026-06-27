# Fluxo de Caixa — Controle de Lançamentos e Consolidado Diário

Solução de referência para o desafio de **Arquiteto de Software**: um comerciante controla
seu fluxo de caixa diário registrando **lançamentos** (débitos e créditos) e consulta o
**saldo diário consolidado**.

A arquitetura prioriza **escalabilidade, resiliência, segurança e desempenho**, com dois
serviços **desacoplados por mensageria assíncrona** — de modo que o serviço de Lançamentos
**permanece disponível mesmo se o de Consolidado cair**, e o caminho de leitura sustenta
picos de **50 req/s**.

> 📁 Documentação completa em [`docs/`](docs/): [arquitetura e diagramas](docs/architecture.md) ·
> [ADRs](docs/adr/) · [requisitos não-funcionais](docs/non-functional.md) · [evoluções futuras](docs/future.md).

---

## Visão geral da arquitetura

```
  POST /lancamentos                                          GET /consolidado/{comerciante}/{data}
        │                                                                    ▲
        ▼                                                                    │
┌───────────────────┐    grava lançamento + evento        ┌──────────────────────────────────┐
│  Lançamentos.API  │──── (mesma transação / Outbox) ────▶ │         PostgreSQL (Lançamentos) │
│  Clean Arch+CQRS  │                                      └──────────────────────────────────┘
│  + OutboxDispatcher│─── publica ──▶ ┌───────────────┐
└───────────────────┘                │   RabbitMQ    │   exchange topic + fila durável + DLQ
                                      └───────────────┘
                                              │ consumo assíncrono e idempotente
                                              ▼
┌────────────────────┐   projeta saldo   ┌──────────────────────┐      ┌───────────────────┐
│ Consolidado.Worker │──────────────────▶│ PostgreSQL(Consolid.)│      │  Consolidado.API  │
│ consumer + Hangfire│                   └──────────────────────┘      │  leitura + cache  │
│ (fechamento diário)│                            ▲                    └─────────┬─────────┘
└────────────────────┘                            │   read-through              │
                                                  └──────── Redis ◀──────────────┘
```

**Por que assíncrono?** É a decisão central do desafio: Lançamentos nunca chama o Consolidado
diretamente. Se o Consolidado cair, os eventos ficam no **Outbox** e na **fila durável** e são
processados no catch-up quando ele voltar. Detalhes em [`docs/architecture.md`](docs/architecture.md).

---

## Stack

| Camada | Tecnologia |
|---|---|
| Linguagem / runtime | **C# / .NET 8** (Minimal APIs) |
| Arquitetura | Clean Architecture, DDD, **CQRS** (MediatR), Outbox, Result pattern |
| Mensageria | **RabbitMQ** (exchange topic, fila durável, DLQ) |
| Persistência | **PostgreSQL** (database-per-service), EF Core |
| Cache | **Redis** (read-through no consolidado) |
| Jobs | **Hangfire** (fechamento diário + reconciliação, no Worker) |
| Resiliência | **Polly** (retry + circuit breaker), consumer idempotente, dead-letter |
| Segurança | **JWT** Bearer, autorização por policy, validação (FluentValidation) |
| Observabilidade | Serilog, health checks, rate limiting |
| Testes | xUnit, FluentAssertions, NSubstitute, **Testcontainers** |
| Infra | Docker / Docker Compose, GitHub Actions (CI), Terraform (IaC ilustrativa) |

---

## Como rodar localmente

### Pré-requisitos
- [Docker](https://www.docker.com/) e Docker Compose
- (Opcional, para rodar testes/serviços fora de container) [.NET 8 SDK](https://dotnet.microsoft.com/download)

### Subir tudo com um comando

```bash
docker compose up --build
```

Isso sobe: 2 PostgreSQL, RabbitMQ, Redis, as 2 APIs e o Worker. As migrations são aplicadas
automaticamente na subida.

### Endpoints

| Serviço | URL | Descrição |
|---|---|---|
| Lançamentos — Swagger | http://localhost:8081/swagger | Registrar/consultar lançamentos |
| Consolidado — Swagger | http://localhost:8082/swagger | Consultar saldo consolidado |
| Hangfire Dashboard | http://localhost:8083/hangfire | Jobs recorrentes (fechamento/reconciliação) |
| RabbitMQ Management | http://localhost:15672 | Filas e mensagens (guest/guest) |
| Health checks | `/health` em 8081 e 8082 | Liveness/readiness |

### Fluxo de exemplo (curl)

```bash
# 1) Autenticar e obter um JWT (endpoint de demonstração)
TOKEN=$(curl -s -X POST http://localhost:8081/auth/token \
  -H "Content-Type: application/json" \
  -d '{"comercianteId":"loja-001"}' | python3 -c "import sys,json;print(json.load(sys.stdin)['access_token'])")

# 2) Registrar lançamentos (tipo 1 = crédito, 2 = débito) — respondem 202 Accepted
curl -X POST http://localhost:8081/lancamentos -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" -d '{"comercianteId":"loja-001","valor":100,"tipo":1}'
curl -X POST http://localhost:8081/lancamentos -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" -d '{"comercianteId":"loja-001","valor":30,"tipo":2}'

# 3) Consultar o saldo consolidado do dia (aguarde ~5s pela consolidação assíncrona)
curl http://localhost:8082/consolidado/loja-001/$(date -u +%Y-%m-%d) -H "Authorization: Bearer $TOKEN"
# => { ... "saldo": 70.00, "quantidadeLancamentos": 2 ... }
```

### Demonstrar a resiliência (NFR central)

```bash
docker compose stop consolidado-worker          # derruba o consumidor
# POSTs em /lancamentos continuam retornando 202 (Lançamentos segue disponível)
# as mensagens acumulam na fila do RabbitMQ
docker compose start consolidado-worker         # catch-up automático: o saldo se atualiza
```

---

## Rodar os testes

```bash
dotnet test
```

- **Unitários**: regras de domínio, handlers CQRS, idempotência, cache.
- **Integração** (Testcontainers): sobe Postgres + RabbitMQ + Redis reais e exercita o fluxo
  assíncrono ponta a ponta. **Requer Docker em execução.**

---

## Estrutura do repositório

```
src/
  BuildingBlocks/        Contratos de integração (shared kernel), Result pattern, abstrações
  SharedKernel/          Autenticação JWT e tratamento global de exceções (reuso entre hosts)
  Lancamentos/           Domain · Application · Infrastructure · API   (+ Outbox)
  Consolidado/           Domain · Application · Infrastructure · API · Worker  (+ Hangfire)
tests/
  Lancamentos.UnitTests  Consolidado.UnitTests  IntegrationTests
docs/                    Arquitetura (C4 + Mermaid), ADRs, NFRs, evoluções
terraform/               IaC ilustrativa (Azure)
docker-compose.yml       Orquestração local completa
.github/workflows/ci.yml CI (build + testes + build de imagens)
```

Cada serviço segue **Clean Architecture** (dependências apontando para dentro):
`API → Infrastructure → Application → Domain`.

---

## Decisões de arquitetura (resumo)

| # | Decisão | Por quê |
|---|---|---|
| [0001](docs/adr/0001-mensageria-assincrona.md) | Comunicação assíncrona via RabbitMQ | Desacoplar Lançamentos do Consolidado (NFR central) |
| [0002](docs/adr/0002-database-per-service.md) | Database-per-service | Independência de dados e de falhas |
| [0003](docs/adr/0003-padrao-outbox.md) | Padrão Outbox | Evitar perda de evento (dual-write) |
| [0004](docs/adr/0004-cache-consolidado.md) | Projeção + cache no consolidado | Sustentar 50 req/s na leitura |
| [0005](docs/adr/0005-cqrs.md) | CQRS | Separar escrita (lançar) de leitura (consultar) |
| [0006](docs/adr/0006-hangfire-worker.md) | Hangfire isolado no Worker | Fechamento diário sem afetar a escala de leitura |

Veja também os [requisitos não-funcionais e métricas](docs/non-functional.md) e as
[evoluções futuras](docs/future.md).
