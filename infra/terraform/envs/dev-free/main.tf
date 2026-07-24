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
  container_app_http_port  = 8080
  container_image_owner    = "nikitasychou"
  container_image_registry = "ghcr.io"
  container_images = {
    gateway          = "${local.container_image_registry}/${local.container_image_owner}/logisticshub-gateway:${var.container_image_tag}"
    cacheworker      = "${local.container_image_registry}/${local.container_image_owner}/logisticshub-cacheworker:${var.container_image_tag}"
    companyservice   = "${local.container_image_registry}/${local.container_image_owner}/logisticshub-companyservice:${var.container_image_tag}"
    inventoryservice = "${local.container_image_registry}/${local.container_image_owner}/logisticshub-inventoryservice:${var.container_image_tag}"
    shipmentservice  = "${local.container_image_registry}/${local.container_image_owner}/logisticshub-shipmentservice:${var.container_image_tag}"
  }

  common_container_environment = {
    ASPNETCORE_ENVIRONMENT = "Production"
    DOTNET_ENVIRONMENT     = "Production"
  }

  azure_ad_environment = {
    AzureAd__Instance      = var.azure_ad_instance
    AzureAd__TenantId      = var.azure_ad_tenant_id
    AzureAd__ClientId      = var.azure_ad_client_id
    AzureAd__Audience      = var.azure_ad_audience
    AzureAd__RequiredScope = var.azure_ad_required_scope
  }

  company_service_resilience_environment = {
    CompanyService__Resilience__TimeoutSeconds                 = "3"
    CompanyService__Resilience__RetryCount                     = "1"
    CompanyService__Resilience__RetryDelayMilliseconds         = "150"
    CompanyService__Resilience__CircuitBreakerFailureThreshold = "3"
    CompanyService__Resilience__CircuitBreakerDurationSeconds  = "5"
  }

  rabbitmq_image = "docker.io/library/rabbitmq:4.1.4-alpine@sha256:b736d649308e1b3e1a116c3f36986b605ee3d03e88f10166be2900083d2e63f2"
  rabbitmq_port  = 5672
  redis_image    = "docker.io/library/redis:8.2.1-alpine@sha256:987c376c727652f99625c7d205a1cba3cb2c53b92b0b62aade2bd48ee1593232"
  redis_port     = 6379

  gateway_reverse_proxy_environment = {
    ReverseProxy__Clusters__company-cluster__Destinations__company-destination__Address     = "https://${azurerm_container_app.companyservice.ingress[0].fqdn}/"
    ReverseProxy__Clusters__inventory-cluster__Destinations__inventory-destination__Address = "https://${azurerm_container_app.inventoryservice.ingress[0].fqdn}/"
    ReverseProxy__Clusters__shipment-cluster__Destinations__shipment-destination__Address   = "https://${azurerm_container_app.shipmentservice.ingress[0].fqdn}/"
  }

  gateway_cors_environment = {
    Cors__AllowedOrigins__0 = trimsuffix(azurerm_storage_account.frontend.primary_web_endpoint, "/")
  }

  rabbitmq_environment = {
    RabbitMq__HostName              = azurerm_container_app.rabbitmq.name
    RabbitMq__Port                  = tostring(local.rabbitmq_port)
    RabbitMq__UserName              = var.rabbitmq_username
    RabbitMq__ExchangeName          = "logisticshub.events"
    RabbitMq__ConsumerPrefetchCount = "1"
  }

  redis_connection_string = "${azurerm_container_app.redis.ingress[0].fqdn}:${local.redis_port},password=${var.redis_password},ssl=False,abortConnect=False,connectRetry=5,connectTimeout=10000,syncTimeout=10000"

  sql_database_max_size_bytes = 34359738368
  sql_database_names = {
    company   = "CompanyDb"
    inventory = "InventoryDb"
    shipment  = "ShipmentDb"
  }
  sql_connection_strings = {
    company   = "Server=tcp:${azurerm_mssql_server.main.fully_qualified_domain_name},1433;Database=${azapi_resource.sql_database["company"].name};User ID=${var.sql_administrator_login};Password=${var.sql_administrator_login_password};Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;"
    inventory = "Server=tcp:${azurerm_mssql_server.main.fully_qualified_domain_name},1433;Database=${azapi_resource.sql_database["inventory"].name};User ID=${var.sql_administrator_login};Password=${var.sql_administrator_login_password};Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;"
    shipment  = "Server=tcp:${azurerm_mssql_server.main.fully_qualified_domain_name},1433;Database=${azapi_resource.sql_database["shipment"].name};User ID=${var.sql_administrator_login};Password=${var.sql_administrator_login_password};Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;"
  }
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

resource "azurerm_mssql_server" "main" {
  name                          = var.sql_server_name
  resource_group_name           = azurerm_resource_group.main.name
  location                      = azurerm_resource_group.main.location
  version                       = "12.0"
  administrator_login           = var.sql_administrator_login
  administrator_login_password  = var.sql_administrator_login_password
  minimum_tls_version           = "1.2"
  public_network_access_enabled = true
  tags                          = var.tags
}

resource "azurerm_mssql_firewall_rule" "allow_azure_services" {
  name             = "AllowAzureServices"
  server_id        = azurerm_mssql_server.main.id
  start_ip_address = "0.0.0.0"
  end_ip_address   = "0.0.0.0"
}

resource "azapi_resource" "sql_database" {
  for_each  = local.sql_database_names
  type      = "Microsoft.Sql/servers/databases@2023-08-01"
  name      = each.value
  parent_id = azurerm_mssql_server.main.id
  location  = azurerm_resource_group.main.location
  tags      = var.tags

  body = {
    sku = {
      name     = "GP_S_Gen5"
      tier     = "GeneralPurpose"
      family   = "Gen5"
      capacity = 2
    }

    properties = {
      collation                        = "SQL_Latin1_General_CP1_CI_AS"
      maxSizeBytes                     = local.sql_database_max_size_bytes
      autoPauseDelay                   = 60
      minCapacity                      = 0.5
      requestedBackupStorageRedundancy = "Local"
      useFreeLimit                     = true
      freeLimitExhaustionBehavior      = "AutoPause"
    }
  }
}

resource "azurerm_container_app" "companyservice" {
  name                         = var.companyservice_container_app_name
  container_app_environment_id = azurerm_container_app_environment.main.id
  resource_group_name          = azurerm_resource_group.main.name
  workload_profile_name        = "Consumption"
  revision_mode                = "Single"
  tags                         = var.tags

  secret {
    name  = "companydb-connection-string"
    value = local.sql_connection_strings.company
  }

  secret {
    name  = "redis-connection-string"
    value = local.redis_connection_string
  }

  ingress {
    external_enabled = false
    target_port      = local.container_app_http_port
    transport        = "http"

    traffic_weight {
      latest_revision = true
      percentage      = 100
    }
  }

  template {
    min_replicas = 1
    max_replicas = 1

    container {
      name   = "companyservice"
      image  = local.container_images.companyservice
      cpu    = 0.25
      memory = "0.5Gi"

      dynamic "env" {
        for_each = merge(local.common_container_environment, local.azure_ad_environment)
        iterator = plain_env

        content {
          name  = plain_env.key
          value = plain_env.value
        }
      }

      dynamic "env" {
        for_each = {
          ConnectionStrings__CompanyDb = "companydb-connection-string"
          ConnectionStrings__Redis     = "redis-connection-string"
        }
        iterator = secret_env

        content {
          name        = secret_env.key
          secret_name = secret_env.value
        }
      }

      startup_probe {
        transport               = "HTTP"
        path                    = "/health/live"
        port                    = local.container_app_http_port
        initial_delay           = 15
        interval_seconds        = 10
        timeout                 = 5
        failure_count_threshold = 18
      }

      readiness_probe {
        transport               = "HTTP"
        path                    = "/health/ready"
        port                    = local.container_app_http_port
        interval_seconds        = 10
        timeout                 = 5
        failure_count_threshold = 6
        success_count_threshold = 1
      }

      liveness_probe {
        transport               = "HTTP"
        path                    = "/health/live"
        port                    = local.container_app_http_port
        initial_delay           = 30
        interval_seconds        = 30
        timeout                 = 5
        failure_count_threshold = 3
      }
    }
  }
}

resource "azurerm_container_app" "inventoryservice" {
  name                         = var.inventoryservice_container_app_name
  container_app_environment_id = azurerm_container_app_environment.main.id
  resource_group_name          = azurerm_resource_group.main.name
  workload_profile_name        = "Consumption"
  revision_mode                = "Single"
  tags                         = var.tags

  secret {
    name  = "inventorydb-connection-string"
    value = local.sql_connection_strings.inventory
  }

  secret {
    name  = "rabbitmq-password"
    value = var.rabbitmq_password
  }

  ingress {
    external_enabled = false
    target_port      = local.container_app_http_port
    transport        = "http"

    traffic_weight {
      latest_revision = true
      percentage      = 100
    }
  }

  template {
    min_replicas = 1
    max_replicas = 1

    container {
      name   = "inventoryservice"
      image  = local.container_images.inventoryservice
      cpu    = 0.5
      memory = "1Gi"

      dynamic "env" {
        for_each = merge(local.common_container_environment, local.azure_ad_environment, local.rabbitmq_environment)
        iterator = plain_env

        content {
          name  = plain_env.key
          value = plain_env.value
        }
      }

      dynamic "env" {
        for_each = {
          ConnectionStrings__InventoryDb = "inventorydb-connection-string"
          RabbitMq__Password             = "rabbitmq-password"
        }
        iterator = secret_env

        content {
          name        = secret_env.key
          secret_name = secret_env.value
        }
      }

      startup_probe {
        transport               = "HTTP"
        path                    = "/health/live"
        port                    = local.container_app_http_port
        initial_delay           = 15
        interval_seconds        = 10
        timeout                 = 5
        failure_count_threshold = 18
      }

      readiness_probe {
        transport               = "HTTP"
        path                    = "/health/ready"
        port                    = local.container_app_http_port
        interval_seconds        = 10
        timeout                 = 5
        failure_count_threshold = 6
        success_count_threshold = 1
      }

      liveness_probe {
        transport               = "HTTP"
        path                    = "/health/live"
        port                    = local.container_app_http_port
        initial_delay           = 30
        interval_seconds        = 30
        timeout                 = 5
        failure_count_threshold = 3
      }
    }
  }
}

resource "azurerm_container_app" "shipmentservice" {
  name                         = var.shipmentservice_container_app_name
  container_app_environment_id = azurerm_container_app_environment.main.id
  resource_group_name          = azurerm_resource_group.main.name
  workload_profile_name        = "Consumption"
  revision_mode                = "Single"
  tags                         = var.tags

  secret {
    name  = "shipmentdb-connection-string"
    value = local.sql_connection_strings.shipment
  }

  secret {
    name  = "rabbitmq-password"
    value = var.rabbitmq_password
  }

  ingress {
    external_enabled = false
    target_port      = local.container_app_http_port
    transport        = "http"

    traffic_weight {
      latest_revision = true
      percentage      = 100
    }
  }

  template {
    min_replicas = 1
    max_replicas = 1

    container {
      name   = "shipmentservice"
      image  = local.container_images.shipmentservice
      cpu    = 0.5
      memory = "1Gi"

      dynamic "env" {
        for_each = merge(
          local.common_container_environment,
          local.azure_ad_environment,
          local.rabbitmq_environment,
          local.company_service_resilience_environment,
          {
            CompanyService__BaseUrl = "https://${azurerm_container_app.companyservice.ingress[0].fqdn}"
          }
        )
        iterator = plain_env

        content {
          name  = plain_env.key
          value = plain_env.value
        }
      }

      dynamic "env" {
        for_each = {
          ConnectionStrings__ShipmentDb = "shipmentdb-connection-string"
          RabbitMq__Password            = "rabbitmq-password"
        }
        iterator = secret_env

        content {
          name        = secret_env.key
          secret_name = secret_env.value
        }
      }

      startup_probe {
        transport               = "HTTP"
        path                    = "/health/live"
        port                    = local.container_app_http_port
        initial_delay           = 15
        interval_seconds        = 10
        timeout                 = 5
        failure_count_threshold = 18
      }

      readiness_probe {
        transport               = "HTTP"
        path                    = "/health/ready"
        port                    = local.container_app_http_port
        interval_seconds        = 10
        timeout                 = 5
        failure_count_threshold = 6
        success_count_threshold = 1
      }

      liveness_probe {
        transport               = "HTTP"
        path                    = "/health/live"
        port                    = local.container_app_http_port
        initial_delay           = 30
        interval_seconds        = 30
        timeout                 = 5
        failure_count_threshold = 3
      }
    }
  }
}

resource "azurerm_container_app" "cacheworker" {
  name                         = var.cacheworker_container_app_name
  container_app_environment_id = azurerm_container_app_environment.main.id
  resource_group_name          = azurerm_resource_group.main.name
  workload_profile_name        = "Consumption"
  revision_mode                = "Single"
  tags                         = var.tags

  secret {
    name  = "companydb-connection-string"
    value = local.sql_connection_strings.company
  }

  secret {
    name  = "redis-connection-string"
    value = local.redis_connection_string
  }

  template {
    min_replicas = 1
    max_replicas = 1

    container {
      name   = "cacheworker"
      image  = local.container_images.cacheworker
      cpu    = 0.25
      memory = "0.5Gi"

      dynamic "env" {
        for_each = merge(
          local.common_container_environment,
          {
            CacheWorker__RunOnStartup                                        = "true"
            CacheWorker__RunOnce                                             = "false"
            CacheWorker__RefreshInterval                                     = "24:00:00"
            CacheWorker__StartupJitterPercentage                             = "5"
            CacheWorker__RefreshJitterPercentage                             = "5"
            CacheWorker__GlobalTimeout                                       = "00:30:00"
            CacheWorker__MaxDegreeOfParallelism                              = "2"
            CacheWorker__EnabledModules__0                                   = "companies"
            CacheWorker__EnabledModules__1                                   = "company-addresses"
            CompanyCacheWarmup__BatchSize                                    = "500"
            CompanyCacheWarmup__ConsecutiveCacheWriteFailureThreshold        = "10"
            CompanyAddressCacheWarmup__BatchSize                             = "500"
            CompanyAddressCacheWarmup__ConsecutiveCacheWriteFailureThreshold = "10"
          }
        )
        iterator = plain_env

        content {
          name  = plain_env.key
          value = plain_env.value
        }
      }

      dynamic "env" {
        for_each = {
          ConnectionStrings__CompanyDb = "companydb-connection-string"
          ConnectionStrings__Redis     = "redis-connection-string"
        }
        iterator = secret_env

        content {
          name        = secret_env.key
          secret_name = secret_env.value
        }
      }
    }
  }
}
resource "azurerm_container_app" "gateway" {
  name                         = var.gateway_container_app_name
  container_app_environment_id = azurerm_container_app_environment.main.id
  resource_group_name          = azurerm_resource_group.main.name
  workload_profile_name        = "Consumption"
  revision_mode                = "Single"
  tags                         = var.tags

  ingress {
    external_enabled           = true
    allow_insecure_connections = false
    target_port                = local.container_app_http_port
    transport                  = "http"

    traffic_weight {
      latest_revision = true
      percentage      = 100
    }
  }

  template {
    min_replicas = 1
    max_replicas = 1

    container {
      name   = "gateway"
      image  = local.container_images.gateway
      cpu    = 0.25
      memory = "0.5Gi"

      dynamic "env" {
        for_each = merge(local.common_container_environment, local.azure_ad_environment, local.gateway_reverse_proxy_environment, local.gateway_cors_environment)
        iterator = plain_env

        content {
          name  = plain_env.key
          value = plain_env.value
        }
      }

      startup_probe {
        transport               = "HTTP"
        path                    = "/health/live"
        port                    = local.container_app_http_port
        initial_delay           = 15
        interval_seconds        = 10
        timeout                 = 5
        failure_count_threshold = 18
      }

      readiness_probe {
        transport               = "HTTP"
        path                    = "/health/ready"
        port                    = local.container_app_http_port
        interval_seconds        = 10
        timeout                 = 5
        failure_count_threshold = 6
        success_count_threshold = 1
      }

      liveness_probe {
        transport               = "HTTP"
        path                    = "/health/live"
        port                    = local.container_app_http_port
        initial_delay           = 30
        interval_seconds        = 30
        timeout                 = 5
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
