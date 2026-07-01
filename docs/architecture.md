# Arquitetura — Fluxo de Caixa

Este documento descreve a arquitetura usando o **C4 Model** (níveis de Contexto e Container)
e diagramas de sequência. Os diagramas são em **Mermaid** (renderizam no GitHub).

## Índice
1. [C4 — Nível 1: Contexto](#c4--nível-1-contexto)
2. [C4 — Nível 2: Containers](#c4--nível-2-containers)
3. [C4 — Nível 3: Componentes (por serviço)](#c4--nível-3-componentes-por-serviço)
4. [Fluxo: registrar lançamento](#fluxo-registrar-lançamento)
5. [Fluxo: consultar consolidado](#fluxo-consultar-consolidado)
6. [Fluxo: resiliência (consolidado indisponível)](#fluxo-resiliência-consolidado-indisponível)
7. [Modelo de domínio](#modelo-de-domínio)

---

## C4 — Nível 1: Contexto

```mermaid
flowchart TB
    comerciante["Comerciante<br/>(cliente da API)"]

    subgraph sistema["Sistema de Fluxo de Caixa"]
        lanc["Serviço de Lançamentos<br/>(débitos e créditos)"]
        cons["Serviço de Consolidado Diário<br/>(saldo consolidado)"]
    end

    comerciante -->|"registra lançamentos<br/>(HTTPS/JWT)"| lanc
    comerciante -->|"consulta saldo diário<br/>(HTTPS/JWT)"| cons
    lanc -.->|"eventos de lançamento<br/>(assíncrono)"| cons
```

O comerciante interage com **dois serviços independentes**. A comunicação entre eles é
**assíncrona** (linha tracejada): o Consolidado é eventualmente consistente com os lançamentos.

---

## C4 — Nível 2: Containers

```mermaid
flowchart TB
    cliente["Comerciante"]

    subgraph svcLanc["Serviço de Lançamentos"]
        apiL["Lançamentos.API<br/>[.NET 8 Minimal API]<br/>registra lançamentos · Outbox dispatcher"]
    end

    broker{{"RabbitMQ<br/>exchange topic · fila durável · DLQ"}}

    subgraph svcCons["Serviço de Consolidado"]
        worker["Consolidado.Worker<br/>[.NET 8]<br/>consumer idempotente · Hangfire"]
        apiC["Consolidado.API<br/>[.NET 8 Minimal API]<br/>leitura do saldo"]
        cache[("Redis<br/>saldo cacheado")]
    end

    subgraph pg["PostgreSQL (database-per-service lógico)"]
        dbL[("db lancamentos<br/>lançamentos + outbox")]
        dbC[("db consolidado<br/>saldos + idempotência")]
    end

    cliente -->|"POST /lancamentos"| apiL
    cliente -->|"GET /consolidado"| apiC
    apiL -->|"grava lançamento + outbox<br/>(1 transação)"| dbL
    apiL -->|"publica evento"| broker
    broker -->|"consome"| worker
    worker -->|"projeta saldo"| dbC
    worker -->|"write-through"| cache
    apiC -->|"lê (read-through)"| cache
    apiC -->|"miss → projeção"| dbC
```

**Pontos-chave:**
- **Database-per-service (lógico):** cada serviço é dono do seu banco (`lancamentos`,
  `consolidado`), sem acesso cruzado. Em dev compartilham um mesmo servidor PostgreSQL; a
  separação física em instâncias dedicadas é só trocar o `Host` (ver [ADR 0002](adr/0002-database-per-service.md)).
- **Outbox dispatcher** roda dentro da `Lançamentos.API` (publicação confiável).
- **Worker** e **Consolidado.API** são deployables separados: o Worker consome e roda jobs;
  a API só lê. Assim a leitura (50 req/s) escala independentemente do consumo.

---

## C4 — Nível 3: Componentes (por serviço)

Cada serviço segue **Clean Architecture** — dependências sempre apontando para dentro:

```mermaid
flowchart LR
    subgraph API["API / Worker"]
        ep["Endpoints / Consumer / Jobs"]
    end
    subgraph App["Application (casos de uso)"]
        cqrs["Commands/Queries<br/>(MediatR) · Validators · Ports"]
    end
    subgraph Dom["Domain"]
        agg["Aggregates · Value Objects<br/>· regras de negócio"]
    end
    subgraph Infra["Infrastructure"]
        adapters["EF Core · RabbitMQ · Redis<br/>· Outbox · implementações das Ports"]
    end

    ep --> cqrs
    cqrs --> agg
    adapters --> cqrs
    adapters --> agg
    ep --> adapters
```

A camada de **Application define as Ports** (interfaces: repositórios, Outbox, cache, clock);
a **Infrastructure as implementa** (Adapters). Domínio não depende de nada externo.

---

## Fluxo: registrar lançamento

```mermaid
sequenceDiagram
    autonumber
    participant C as Comerciante
    participant API as Lançamentos.API
    participant DB as PostgreSQL Lanç.
    participant OD as OutboxDispatcher
    participant MQ as RabbitMQ

    C->>API: POST /lancamentos (JWT)
    API->>API: valida (FluentValidation) + cria aggregate
    API->>DB: BEGIN TX
    API->>DB: INSERT lancamento
    API->>DB: INSERT outbox_message (evento)
    API->>DB: COMMIT (atômico)
    API-->>C: 202 Accepted (Id)
    Note over OD,MQ: assíncrono, desacoplado da requisição
    OD->>DB: SELECT pendentes (FOR UPDATE SKIP LOCKED)
    OD->>MQ: publica evento (persistente + confirms)
    OD->>DB: marca como processado
```

O cliente recebe **202** assim que o lançamento é persistido. A publicação no broker é
responsabilidade do dispatcher, em segundo plano — por isso uma falha do broker **não afeta**
a resposta ao cliente.

---

## Fluxo: consultar consolidado

```mermaid
sequenceDiagram
    autonumber
    participant MQ as RabbitMQ
    participant W as Consolidado.Worker
    participant DB as PostgreSQL Consol.
    participant R as Redis
    participant API as Consolidado.API
    participant C as Comerciante

    MQ->>W: entrega evento de lançamento
    W->>DB: evento já processado? (idempotência)
    alt novo evento
        W->>DB: aplica crédito/débito na projeção (1 TX: saldo + marca)
        W->>R: write-through (grava saldo autoritativo no cache)
        W->>MQ: ACK
    else duplicado
        W->>MQ: ACK (ignora)
    end

    C->>API: GET /consolidado/:comerciante/:data (JWT)
    API->>R: lê do cache
    alt cache hit
        R-->>API: saldo
    else cache miss
        API->>DB: lê projeção
        API->>R: popula cache
    end
    API-->>C: 200 (saldo consolidado)
```

A leitura é servida por uma **projeção pré-calculada** + **cache** — nunca recalcula somando
lançamentos. É isso que sustenta o pico de 50 req/s.

---

## Fluxo: resiliência (consolidado indisponível)

```mermaid
sequenceDiagram
    autonumber
    participant C as Comerciante
    participant API as Lançamentos.API
    participant DB as PostgreSQL Lanç.
    participant MQ as RabbitMQ
    participant W as Consolidado.Worker

    Note over W: Worker/Consolidado fora do ar (falha)
    C->>API: POST /lancamentos
    API->>DB: grava lançamento + outbox
    API-->>C: 202 Accepted (segue disponível)
    Note over MQ: mensagens acumulam na fila durável
    Note over W: Worker volta (catch-up)
    W->>MQ: consome o backlog (catch-up)
    W->>DB: projeta os saldos pendentes
```

O serviço de Lançamentos depende apenas do **seu** banco e do broker. A indisponibilidade do
Consolidado **não o derruba** — comprovado pelo teste de integração e pelo roteiro de
resiliência no [README](../README.md#demonstrar-a-resiliência-nfr-central).

---

## Modelo de domínio

**Lançamentos**
- `Lancamento` (aggregate root): comerciante, `Money` (VO, valor positivo), tipo (crédito/débito),
  data, descrição. Imutável após criado (fato contábil).

**Consolidado**
- `SaldoDiario` (projeção): totais de crédito/débito, saldo, quantidade, flag de fechamento.
  Atualizado incrementalmente a cada evento; concorrência otimista via `xmin`.
- `EventoProcessado`: chave de idempotência (EventId) gravada junto com a projeção.

Os tipos do contrato de integração (`BuildingBlocks.Contracts`) são propositalmente
**separados** dos enums de domínio — a camada de Application faz o mapeamento, preservando a
fronteira entre o modelo interno e o contrato externo.
