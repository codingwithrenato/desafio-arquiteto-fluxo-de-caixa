# Infraestrutura como Código (Terraform) — ilustrativa

> ⚠️ **Esboço ilustrativo.** Demonstra como a solução seria provisionada no Azure.
> Não é aplicado pelo desafio (requer assinatura, credenciais e backend de state).

## O que este IaC provisiona

| Recurso | Papel |
|---|---|
| `azurerm_resource_group` | Agrupamento lógico do ambiente |
| `azurerm_log_analytics_workspace` | Observabilidade (logs/metrics dos Container Apps) |
| `azurerm_postgresql_flexible_server` + 2 databases | Persistência **database-per-service** (lancamentos, consolidado) |
| `azurerm_redis_cache` | Cache distribuído do consolidado |
| `azurerm_key_vault` | Segredos (connection strings, chave JWT) — fora do código/imagem |
| `azurerm_container_app_environment` | Ambiente serverless dos containers |
| 3x `azurerm_container_app` | Lançamentos.API, Consolidado.API e Consolidado.Worker |

## Decisões de arquitetura refletidas no IaC

- **Escala horizontal independente:** a `consolidado-api` (leitura) escala até 20 réplicas
  por requisições concorrentes (pico de 50 req/s); o `consolidado-worker` escala
  separadamente (até 5) pela profundidade da fila. Isso materializa a separação
  read-API / job-worker (ver `docs/adr/0006`).
- **Alta disponibilidade:** `min_replicas = 2` nos serviços de borda.
- **Segurança:** segredos em Key Vault, TLS mínimo 1.2 no Redis.
- **Mensageria:** em produção, o RabbitMQ pode ser substituído pelo **Azure Service Bus**
  (gerenciado) — ver `docs/adr/0001`. Mantém-se o desacoplamento assíncrono.

## Como seria aplicado

```bash
terraform init
terraform plan  -var="tenant_id=..." -var="registry=acr.azurecr.io" -var="pg_admin_password=..."
terraform apply -var="tenant_id=..." -var="registry=acr.azurecr.io" -var="pg_admin_password=..."
```
