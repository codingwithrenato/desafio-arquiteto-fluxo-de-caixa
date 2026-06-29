# Requisitos Não-Funcionais — Métricas, Metas e Estratégias

Este documento define **metas mensuráveis** para os atributos de qualidade e como a arquitetura
os endereça. Os números são alvos de referência (SLOs) para um ambiente de produção típico.

## 1. Disponibilidade

| Métrica | Meta | Como é atendida |
|---|---|---|
| Disponibilidade do Lançamentos | **≥ 99,9%** | Stateless + N réplicas; **independente do Consolidado e do broker** (Outbox). |
| Disponibilidade do Consolidado (leitura) | ≥ 99,9% | Stateless + N réplicas atrás de load balancer; cache reduz dependência do banco. |
| Acoplamento de disponibilidade | **Zero** entre os serviços | Comunicação assíncrona (ADR 0001); readiness do Lançamentos **não** inclui o broker. |

**Requisito do desafio atendido:** *Lançamentos não fica indisponível se o Consolidado cair* —
comprovado por teste de integração e roteiro de resiliência.

## 2. Desempenho / Escalabilidade

| Métrica | Meta | Como é atendida |
|---|---|---|
| Throughput de leitura do Consolidado | **≥ 50 req/s** (pico) | Projeção pré-calculada + cache Redis (write-through) + escala horizontal. |
| Perda de requisições no pico | **≤ 5%** | Rate limiting (load shedding) devolve 429 controlado em vez de degradar tudo. |
| Latência de leitura (p95) | < 100 ms | Servida do cache; miss vai a uma projeção O(1). |
| Latência de escrita (p95) | < 150 ms | Persistência local + Outbox; sem chamada ao Consolidado no caminho. |
| Consistência do saldo | Eventual, **< 5 s** típico | Intervalo do Outbox dispatcher + consumo da fila. |

**Requisito do desafio atendido:** *50 req/s no Consolidado com até 5% de perda*.

## 3. Confiabilidade / Integridade

| Métrica | Meta | Como é atendida |
|---|---|---|
| Perda de eventos | **0** | Padrão Outbox (at-least-once) + filas/mensagens duráveis + publisher confirms. |
| Duplicação de saldo | **0** | Consumidor idempotente (dedup por `EventId`) na mesma transação da projeção. |
| Mensagens "envenenadas" | Isoladas | Dead-letter queue após falhas persistentes; sem travar a fila. |
| Concorrência na projeção | Sem corrida | Concorrência otimista (`xmin`) no `SaldoDiario`. |
| RPO (perda de dados) | ~0 | Escritas commitadas no PostgreSQL; backups/PITR em produção. |
| RTO (recuperação) | Minutos | Serviços stateless reiniciam rápido; fila preserva o trabalho pendente. |

## 4. Segurança

| Aspecto | Estratégia |
|---|---|
| Autenticação | **JWT Bearer** (HMAC-SHA256); emissão por IdP dedicado em produção. |
| Autorização | Policies/roles por endpoint (`RequireAuthorization`). |
| Transporte | HTTPS/TLS (terminação no ingress/gateway). |
| Segredos | Variáveis de ambiente em dev; **Key Vault / Secrets** em produção (nunca no código). |
| Validação de entrada | FluentValidation → `400 ProblemDetails`. |
| Proteção contra abuso | Rate limiting; superfície mínima (Minimal APIs). |
| Referência | Boas práticas OWASP Top 10 (injeção, autenticação, exposição de dados). |

## 5. Observabilidade

| Recurso | Implementação |
|---|---|
| Logs estruturados | **Serilog** (console; sink central em produção: ELK/App Insights). |
| Health checks | `/health` por serviço (Postgres, Redis conforme o caminho). |
| Monitoramento de jobs | **Dashboard do Hangfire** (histórico, retries, agendamentos). |
| Mensageria | RabbitMQ Management UI (profundidade de fila, DLQ). |
| Próximos passos | Métricas Prometheus + tracing distribuído (OpenTelemetry) — ver [`future.md`](future.md). |

## Resumo de rastreabilidade (requisito → mecanismo)

```
Lançamentos sempre disponível ─────▶ Mensageria assíncrona + Outbox (ADR 0001, 0003)
50 req/s, ≤ 5% de perda ───────────▶ Projeção + cache + rate limiting (ADR 0004)
Escalabilidade ────────────────────▶ Stateless + DB-per-service + read/worker separados (ADR 0002, 0006)
Resiliência / recuperação ─────────▶ Polly, DLQ, idempotência, health checks
Segurança ─────────────────────────▶ JWT, TLS, secrets externos, validação, rate limiting
```
