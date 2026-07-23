resource "azurerm_resource_group" "main" {
  name     = var.resource_group_name
  location = var.location
  tags     = var.tags
}

resource "azurerm_container_app_environment" "main" {
  name                = var.container_app_environment_name
  resource_group_name = azurerm_resource_group.main.name
  location            = azurerm_resource_group.main.location
  tags                = var.tags

  lifecycle {
    # Azure returns the implicit Consumption profile; ignore it to prevent provider drift.
    ignore_changes = [
      workload_profile,
    ]
  }
}

locals {
  rabbitmq_image = "docker.io/library/rabbitmq:4.1.4-alpine@sha256:b736d649308e1b3e1a116c3f36986b605ee3d03e88f10166be2900083d2e63f2"
  rabbitmq_port  = 5672
  redis_image    = "docker.io/library/redis:8.2.1-alpine@sha256:987c376c727652f99625c7d205a1cba3cb2c53b92b0b62aade2bd48ee1593232"
  redis_port     = 6379
}

resource "azurerm_container_app" "rabbitmq" {
  name                         = var.rabbitmq_container_app_name
  container_app_environment_id = azurerm_container_app_environment.main.id
  resource_group_name          = azurerm_resource_group.main.name
  workload_profile_name        = "Consumption"
  revision_mode                = "Single"
  tags                         = var.tags

  secret {
    name  = "rabbitmq-password"
    value = var.rabbitmq_password
  }

  ingress {
    external_enabled = false
    target_port      = local.rabbitmq_port
    exposed_port     = local.rabbitmq_port
    transport        = "tcp"

    traffic_weight {
      latest_revision = true
      percentage      = 100
    }
  }

  template {
    min_replicas = 1
    max_replicas = 1

    container {
      name   = "rabbitmq"
      image  = local.rabbitmq_image
      cpu    = 0.5
      memory = "1Gi"

      env {
        name  = "RABBITMQ_DEFAULT_USER"
        value = var.rabbitmq_username
      }

      env {
        name        = "RABBITMQ_DEFAULT_PASS"
        secret_name = "rabbitmq-password"
      }

      startup_probe {
        transport               = "TCP"
        port                    = local.rabbitmq_port
        initial_delay           = 10
        interval_seconds        = 10
        timeout                 = 5
        failure_count_threshold = 30
      }

      readiness_probe {
        transport               = "TCP"
        port                    = local.rabbitmq_port
        interval_seconds        = 10
        timeout                 = 5
        failure_count_threshold = 3
        success_count_threshold = 1
      }

      liveness_probe {
        transport               = "TCP"
        port                    = local.rabbitmq_port
        initial_delay           = 30
        interval_seconds        = 30
        timeout                 = 5
        failure_count_threshold = 3
      }
    }
  }
}

resource "azurerm_container_app" "redis" {
  name                         = var.redis_container_app_name
  container_app_environment_id = azurerm_container_app_environment.main.id
  resource_group_name          = azurerm_resource_group.main.name
  workload_profile_name        = "Consumption"
  revision_mode                = "Single"
  tags                         = var.tags

  secret {
    name  = "redis-password"
    value = var.redis_password
  }

  ingress {
    external_enabled = false
    target_port      = local.redis_port
    exposed_port     = local.redis_port
    transport        = "tcp"

    traffic_weight {
      latest_revision = true
      percentage      = 100
    }
  }

  template {
    min_replicas = 1
    max_replicas = 1

    container {
      name    = "redis"
      image   = local.redis_image
      cpu     = 0.25
      memory  = "0.5Gi"
      command = ["/bin/sh", "-c"]
      args    = ["exec redis-server --appendonly no --save '' --requirepass \"$REDIS_PASSWORD\""]

      env {
        name        = "REDIS_PASSWORD"
        secret_name = "redis-password"
      }

      startup_probe {
        transport               = "TCP"
        port                    = local.redis_port
        initial_delay           = 5
        interval_seconds        = 5
        timeout                 = 3
        failure_count_threshold = 12
      }

      readiness_probe {
        transport               = "TCP"
        port                    = local.redis_port
        interval_seconds        = 5
        timeout                 = 3
        failure_count_threshold = 3
        success_count_threshold = 1
      }

      liveness_probe {
        transport               = "TCP"
        port                    = local.redis_port
        initial_delay           = 15
        interval_seconds        = 15
        timeout                 = 3
        failure_count_threshold = 3
      }
    }
  }
}

resource "azurerm_storage_account" "frontend" {
  name                            = var.frontend_storage_account_name
  resource_group_name             = azurerm_resource_group.main.name
  location                        = azurerm_resource_group.main.location
  account_kind                    = "StorageV2"
  account_tier                    = "Standard"
  account_replication_type        = "LRS"
  https_traffic_only_enabled      = true
  min_tls_version                 = "TLS1_2"
  public_network_access_enabled   = true
  allow_nested_items_to_be_public = false
  tags                            = var.tags
}

resource "azurerm_storage_account_static_website" "frontend" {
  storage_account_id = azurerm_storage_account.frontend.id
  index_document     = "index.html"
  error_404_document = "index.html"
}
