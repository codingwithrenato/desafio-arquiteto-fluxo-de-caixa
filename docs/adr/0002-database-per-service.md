# ADR 0002 — Database-per-service (isolamento lógico)

**Status:** Aceito

## Contexto
Em uma arquitetura de microsserviços, compartilhar tabelas entre serviços reintroduz
acoplamento: mudanças de schema, contenção e falhas se propagam. O desafio pede escalabilidade
e resiliência com serviços independentes.

É importante separar dois conceitos frequentemente confundidos:
- **Isolamento lógico** (o invariante): cada serviço é dono dos seus dados e nenhum serviço lê
  as tabelas do outro diretamente.
- **Topologia física** (decisão de deployment): em quantos servidores/instâncias esses bancos
  rodam. É um trade-off de custo × isolamento operacional, independente do invariante acima.

## Decisão
Adotar **database-per-service no nível lógico**: cada serviço possui o **seu próprio banco**
(`lancamentos` e `consolidado`), com connection strings distintas e **sem acesso cruzado**. A
única forma de um serviço conhecer dados do outro é via **eventos de integração**.

Quanto à topologia física:
- **Desenvolvimento / este desafio:** os dois bancos rodam em **um único servidor PostgreSQL**
  (bancos separados, não schemas). Reduz consumo de recursos sem abrir mão do isolamento lógico
  (PostgreSQL não faz `JOIN` entre bancos sem `dblink`/FDW).
- **Produção em escala:** basta apontar cada serviço para uma **instância dedicada** — muda só o
  `Host` da connection string, **sem alteração de código**.

## Alternativas consideradas
- **Banco único com tabelas compartilhadas:** acopla os serviços no nível de dados. Anti-padrão.
  Rejeitada.
- **Um servidor, dois schemas (no mesmo database):** mais leve, mas a mesma conexão enxerga os
  dois schemas → brecha para acoplamento acidental, enfraquecendo o invariante. Preferimos
  **bancos separados**, que impedem o `JOIN` cruzado por construção.
- **Duas instâncias físicas sempre (inclusive em dev):** isolamento operacional máximo, porém
  custo de recursos desnecessário no ambiente local (e foi parte do que sobrecarregava o
  Docker Desktop). Reservado para produção.

## Consequências
- ✅ Isolamento de evolução de schema e ausência de `JOIN` cross-service (garantido por bancos
  distintos).
- ✅ Caminho de produção claro: separar fisicamente é configuração, não refatoração.
- ✅ Ambiente local enxuto (um servidor PostgreSQL).
- ⚠️ Em dev, os bancos compartilham recursos (CPU/IO) e ciclo de vida do mesmo servidor — sem o
  isolamento de falhas que instâncias dedicadas dão. Aceitável fora de produção.
- ⚠️ Dados duplicados de forma controlada (o Consolidado mantém sua própria projeção).
