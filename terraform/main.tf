# =============================================================================
# IaC ILUSTRATIVA (Azure) — Fluxo de Caixa
# -----------------------------------------------------------------------------
# Objetivo: demonstrar como a solução seria provisionada em nuvem com IaC.
# NÃO é aplicada pelo desafio (sem backend/credenciais). Mostra as decisões de
# infraestrutura: Container Apps (escala horizontal + KEDA), PostgreSQL gerenciado
# (database-per-service), Redis gerenciado e mensageria.
#
# Em produção, o RabbitMQ pode ser trocado pelo Azure Service Bus (gerenciado) —
# ver docs/adr/0001. Aqui mantemos a paridade com o ambiente local.
# =============================================================================

terraform {
  required_version = ">= 1.5"
  required_providers {
    azurerm = {
      source  = "hashicorp/azurerm"
      version = "~> 3.110"
    }
  }
  # backend "azurerm" { ... }  # state remoto em produção
}

provider "azurerm" {
  features {}
}

resource "azurerm_resource_group" "rg" {
  name     = "rg-fluxo-caixa-${var.environment}"
  location = var.location
}

# ----------------------------- Observabilidade ------------------------------
resource "azurerm_log_analytics_workspace" "logs" {
  name                = "log-fluxo-caixa-${var.environment}"
  location            = azurerm_resource_group.rg.location
  resource_group_name = azurerm_resource_group.rg.name
  sku                 = "PerGB2018"
  retention_in_days   = 30
}

# --------------------------- PostgreSQL (gerenciado) ------------------------
# Database-per-service: um servidor, dois bancos isolados (custo-eficiente em dev;
# em produção podem ser dois servidores para isolamento total).
resource "azurerm_postgresql_flexible_server" "pg" {
  name                   = "psql-fluxo-caixa-${var.environment}"
  resource_group_name    = azurerm_resource_group.rg.name
  location               = azurerm_resource_group.rg.location
  version                = "16"
  administrator_login    = var.pg_admin_login
  administrator_password = var.pg_admin_password
  storage_mb             = 32768
  sku_name               = "GP_Standard_D2s_v3"
  zone                   = "1"
}

resource "azurerm_postgresql_flexible_server_database" "lancamentos" {
  name      = "lancamentos"
  server_id = azurerm_postgresql_flexible_server.pg.id
  charset   = "UTF8"
  collation = "en_US.utf8"
}

resource "azurerm_postgresql_flexible_server_database" "consolidado" {
  name      = "consolidado"
  server_id = azurerm_postgresql_flexible_server.pg.id
  charset   = "UTF8"
  collation = "en_US.utf8"
}

# ------------------------------- Redis (cache) ------------------------------
resource "azurerm_redis_cache" "redis" {
  name                = "redis-fluxo-caixa-${var.environment}"
  location            = azurerm_resource_group.rg.location
  resource_group_name = azurerm_resource_group.rg.name
  capacity            = 1
  family              = "C"
  sku_name            = "Standard"
  minimum_tls_version = "1.2"
}

# ------------------------ Cofre de segredos (Key Vault) ---------------------
# Connection strings, chave JWT e credenciais ficam aqui — nunca no código/imagem.
resource "azurerm_key_vault" "kv" {
  name                = "kv-fluxocaixa-${var.environment}"
  location            = azurerm_resource_group.rg.location
  resource_group_name = azurerm_resource_group.rg.name
  tenant_id           = var.tenant_id
  sku_name            = "standard"
}

# --------------------- Container Apps Environment ---------------------------
resource "azurerm_container_app_environment" "env" {
  name                       = "cae-fluxo-caixa-${var.environment}"
  location                   = azurerm_resource_group.rg.location
  resource_group_name        = azurerm_resource_group.rg.name
  log_analytics_workspace_id = azurerm_log_analytics_workspace.logs.id
}

# ---------------- Serviço de Lançamentos (escala horizontal) ----------------
resource "azurerm_container_app" "lancamentos_api" {
  name                         = "ca-lancamentos-api"
  container_app_environment_id = azurerm_container_app_environment.env.id
  resource_group_name          = azurerm_resource_group.rg.name
  revision_mode                = "Single"

  template {
    min_replicas = 2 # alta disponibilidade
    max_replicas = 10

    container {
      name   = "lancamentos-api"
      image  = "${var.registry}/lancamentos-api:${var.image_tag}"
      cpu    = 0.5
      memory = "1Gi"
    }

    # Escala por requisições HTTP concorrentes (KEDA).
    http_scale_rule {
      name                = "http-scaling"
      concurrent_requests = 100
    }
  }

  ingress {
    external_enabled = true
    target_port      = 8080
    traffic_weight {
      latest_revision = true
      percentage      = 100
    }
  }
}

# ------ Serviço de Consolidado: read-API e job-worker escalam separados ------
resource "azurerm_container_app" "consolidado_api" {
  name                         = "ca-consolidado-api"
  container_app_environment_id = azurerm_container_app_environment.env.id
  resource_group_name          = azurerm_resource_group.rg.name
  revision_mode                = "Single"

  template {
    min_replicas = 2
    max_replicas = 20 # caminho de leitura: pico de 50 req/s

    container {
      name   = "consolidado-api"
      image  = "${var.registry}/consolidado-api:${var.image_tag}"
      cpu    = 0.5
      memory = "1Gi"
    }

    http_scale_rule {
      name                = "http-scaling"
      concurrent_requests = 80
    }
  }

  ingress {
    external_enabled = true
    target_port      = 8080
    traffic_weight {
      latest_revision = true
      percentage      = 100
    }
  }
}

resource "azurerm_container_app" "consolidado_worker" {
  name                         = "ca-consolidado-worker"
  container_app_environment_id = azurerm_container_app_environment.env.id
  resource_group_name          = azurerm_resource_group.rg.name
  revision_mode                = "Single"

  template {
    min_replicas = 1
    max_replicas = 5 # escala pela profundidade da fila (KEDA), independente da read-API

    container {
      name   = "consolidado-worker"
      image  = "${var.registry}/consolidado-worker:${var.image_tag}"
      cpu    = 0.5
      memory = "1Gi"
    }
  }
}
