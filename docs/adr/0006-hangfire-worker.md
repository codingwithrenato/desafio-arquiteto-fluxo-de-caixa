# ADR 0006 — Hangfire para jobs no Worker, isolado da API de leitura

**Status:** Aceito

## Contexto
O enunciado fala em **consolidado diário** — há um conceito natural de **fechamento do dia**
(snapshot/imutabilização do saldo) e de **reconciliação** periódica. Isso pede agendamento
(cron) e jobs recorrentes confiáveis, com observabilidade.

Risco a evitar: colocar o agendador dentro da API que escala para 50 req/s faria cada réplica
da API rodar workers fazendo polling na storage de jobs — carga no banco proporcional ao número
de réplicas, competindo com o caminho de leitura.

## Decisão
Usar **Hangfire** para o **fechamento diário** (job cron) e a **reconciliação** (job recorrente),
hospedado **exclusivamente no `Consolidado.Worker`** — um deployable separado da
`Consolidado.API`. O RabbitMQ continua sendo o backbone de mensageria **entre serviços**
(Hangfire não faz pub/sub entre microsserviços; é agendador in-process).

Assim:
- `Consolidado.API` (leitura) escala por requisições — **sem** Hangfire.
- `Consolidado.Worker` (consumo + jobs) escala por profundidade de fila — **com** Hangfire.

## Alternativas consideradas
- **Hangfire dentro da API:** acopla jobs ao caminho de leitura e adiciona carga de polling
  por réplica. Rejeitada.
- **`BackgroundService` com `PeriodicTimer` artesanal:** funciona, mas sem dashboard, retries
  e visibilidade que o Hangfire dá de graça; e o lock distribuído do Hangfire garante execução
  única de jobs recorrentes no cluster.
- **Substituir RabbitMQ por Hangfire para a comunicação entre serviços:** anti-padrão (storage
  compartilhado reintroduz acoplamento). Rejeitada.

## Consequências
- ✅ Escala de leitura preservada (jobs não competem com o caminho de 50 req/s).
- ✅ Jobs recorrentes com retry, histórico e **dashboard** (`/hangfire`) para operação.
- ✅ Execução única no cluster por disparo (lock distribuído).
- ⚠️ Dependência adicional + tabelas de storage do Hangfire (schema próprio no PostgreSQL).
- ⚠️ O **Outbox dispatcher** permanece como `BackgroundService` artesanal (transparência do
  padrão), e não no Hangfire — decisão deliberada de separar responsabilidades.
