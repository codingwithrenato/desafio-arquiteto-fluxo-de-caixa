# Evoluções Futuras

O desafio pede explicitamente descrever *"o que você gostaria de ter implementado ou evoluções
futuras"*. Dado o tempo limitado, o escopo entregue foca no núcleo arquitetural e nos requisitos
não-funcionais; abaixo estão as evoluções naturais, em ordem aproximada de prioridade.

## 1. Observabilidade completa (OpenTelemetry)
Tracing distribuído ponta a ponta (requisição → Outbox → RabbitMQ → consumo → projeção),
métricas Prometheus e dashboards Grafana. Hoje há logs estruturados (Serilog) e health checks;
o próximo passo é correlacionar tudo com `traceId` cruzando os serviços.

## 2. API Gateway + BFF
Um gateway (YARP, Azure API Management) na borda para roteamento, autenticação centralizada,
rate limiting global e versionamento — removendo essas preocupações dos serviços.

## 3. Kubernetes + autoscaling (HPA/KEDA)
Empacotar em Helm charts e escalar com **HPA** (por CPU/memória) e **KEDA** (a `Consolidado.API`
por requisições; o `Consolidado.Worker` pela **profundidade da fila** do RabbitMQ). O esboço
Terraform já reflete essa separação via Azure Container Apps.

## 4. Event Sourcing no Consolidado
Hoje o `SaldoDiario` é uma projeção incremental. Evoluir para **event sourcing** (com Kafka ou
um event store) permitiria reprocessar projeções do zero, auditar a linha do tempo completa e
criar novas visões (mensal, por categoria) sem migração de dados.

## 5. Saga / processos de longa duração
Para fluxos com mais etapas (ex.: estorno, conciliação bancária, cancelamento de lançamento),
um **padrão Saga** (coreografado por eventos ou orquestrado) coordenaria as compensações de
forma resiliente.

## 6. Autenticação em IdP dedicado
Substituir o endpoint de token de demonstração por um **Identity Provider** real
(Azure AD B2C, Keycloak, IdentityServer) com OAuth 2.0/OIDC, refresh tokens e rotação de chaves.

## 7. Particionamento e retenção
Particionar `lancamentos` e `saldos_diarios` por período (mês) para manter as consultas rápidas
com o crescimento do histórico; política de arquivamento/retenção para dados antigos.

## 8. Estratégia de cache mais rica
Cache warming proativo no fechamento do dia, e cache do "saldo do dia corrente" com atualização
incremental (em vez de invalidação) para eliminar o miss pós-escrita no pico.

## 9. Contratos de evento versionados (schema registry)
Versionar os eventos de integração (ex.: Avro/JSON Schema em um schema registry) para evoluir o
contrato entre serviços sem quebra, com compatibilidade para frente/para trás.

## 10. Testes adicionais
- Testes de carga (k6/JMeter) validando os 50 req/s e a tolerância de 5% de perda.
- Testes de contrato (Pact) entre publisher e consumer.
- Testes de caos (derrubar broker/DB) automatizados no pipeline.

## 11. Hardening de produção
TLS fim a fim entre serviços, network policies, secrets rotacionados automaticamente,
backups com PITR, blue/green ou canary deploys (já citados no CV) e feature flags.
