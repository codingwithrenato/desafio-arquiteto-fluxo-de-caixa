# ADR 0002 — Database-per-service

**Status:** Aceito

## Contexto
Em uma arquitetura de microsserviços, compartilhar um mesmo banco entre serviços reintroduz
acoplamento: mudanças de schema, contenção e falhas se propagam. O desafio pede escalabilidade
e resiliência com serviços independentes.

## Decisão
Cada serviço tem o **seu próprio banco PostgreSQL** (`lancamentos` e `consolidado`), sem acesso
cruzado. A única forma de um serviço conhecer dados do outro é via **eventos de integração**.

## Alternativas consideradas
- **Banco único compartilhado:** mais simples de operar, mas acopla os serviços no nível de
  dados — anti-padrão em microsserviços. Rejeitada.
- **Schemas separados no mesmo servidor:** meio-termo; ainda há ponto único de falha e
  contenção de recursos.

## Consequências
- ✅ Isolamento de falhas e de evolução de schema.
- ✅ Cada banco escala/tuna conforme seu padrão de acesso (Lançamentos = escrita;
  Consolidado = leitura intensiva).
- ⚠️ Dados duplicados de forma controlada (o Consolidado mantém sua própria projeção).
- ⚠️ Sem `JOIN` entre serviços — relatórios cross-service exigem composição via eventos/APIs.
