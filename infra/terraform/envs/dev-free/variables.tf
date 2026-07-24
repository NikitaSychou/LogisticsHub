variable "location" {
  description = "Azure region for the dev-free environment."
  type        = string
  default     = "swedencentral"
}

variable "resource_group_name" {
  description = "Resource group name for the dev-free foundation."
  type        = string
  default     = "rg-logisticshub-dev-free"
}

variable "container_app_environment_name" {
  description = "Azure Container Apps managed environment name for dev-free."
  type        = string
  default     = "cae-logisticshub-dev-free"
}

variable "container_image_tag" {
  description = "Immutable full Git commit SHA tag for the LogisticsHub GHCR images."
  type        = string

  validation {
    condition     = can(regex("^[0-9a-f]{40}$", var.container_image_tag))
    error_message = "container_image_tag must be a 40-character lowercase hexadecimal Git commit SHA."
  }
}

variable "azure_ad_instance" {
  description = "Microsoft Entra authority instance URL for API authentication."
  type        = string
  default     = "https://login.microsoftonline.com"

  validation {
    condition     = can(regex("^https://[^[:space:]]+$", var.azure_ad_instance))
    error_message = "azure_ad_instance must be an HTTPS URL without whitespace."
  }
}

variable "azure_ad_tenant_id" {
  description = "Microsoft Entra tenant ID or tenant domain for API authentication."
  type        = string

  validation {
    condition     = length(trimspace(var.azure_ad_tenant_id)) > 0
    error_message = "azure_ad_tenant_id must be non-empty."
  }
}

variable "azure_ad_client_id" {
  description = "Microsoft Entra API application client ID for API authentication."
  type        = string

  validation {
    condition     = length(trimspace(var.azure_ad_client_id)) > 0
    error_message = "azure_ad_client_id must be non-empty."
  }
}

variable "azure_ad_audience" {
  description = "Expected JWT audience for LogisticsHub API authentication."
  type        = string

  validation {
    condition     = length(trimspace(var.azure_ad_audience)) > 0
    error_message = "azure_ad_audience must be non-empty."
  }
}

variable "azure_ad_required_scope" {
  description = "Delegated scope required by LogisticsHub APIs."
  type        = string

  validation {
    condition     = length(trimspace(var.azure_ad_required_scope)) > 0
    error_message = "azure_ad_required_scope must be non-empty."
  }
}

variable "sql_server_name" {
  description = "Globally unique Azure SQL logical server name for dev-free."
  type        = string
  default     = "sql-logisticshub-dev-free-600544"

  validation {
    condition     = length(var.sql_server_name) <= 63 && can(regex("^[a-z0-9]([a-z0-9-]*[a-z0-9])?$", var.sql_server_name)) && !strcontains(var.sql_server_name, "--")
    error_message = "sql_server_name must be no more than 63 characters, use only lowercase letters, digits, and hyphens, start and end with a letter or digit, and not contain consecutive hyphens."
  }
}

variable "sql_administrator_login" {
  description = "Azure SQL administrator login name for dev-free."
  type        = string
  default     = "logisticshubadmin"

  validation {
    condition     = length(trimspace(var.sql_administrator_login)) >= 3 && can(regex("^[A-Za-z][A-Za-z0-9_-]{2,127}$", var.sql_administrator_login)) && !contains(["admin", "administrator", "guest", "public", "root", "sa"], lower(var.sql_administrator_login))
    error_message = "sql_administrator_login must be 3-128 characters, start with a letter, use only letters, digits, underscores, or hyphens, and must not be a reserved administrator name."
  }
}

variable "sql_administrator_login_password" {
  description = "Azure SQL administrator password for dev-free."
  type        = string
  sensitive   = true
  nullable    = false

  validation {
    condition     = length(trimspace(var.sql_administrator_login_password)) > 0
    error_message = "sql_administrator_login_password must be non-empty."
  }
}

variable "rabbitmq_container_app_name" {
  description = "Name of the dev-free RabbitMQ Container App."
  type        = string
  default     = "ca-rmq-logisticshub-dev-free"

  validation {
    condition     = length(var.rabbitmq_container_app_name) <= 32 && can(regex("^[a-z]([a-z0-9-]*[a-z0-9])?$", var.rabbitmq_container_app_name)) && !strcontains(var.rabbitmq_container_app_name, "--")
    error_message = "rabbitmq_container_app_name must be no more than 32 characters, use only lowercase letters, digits, and hyphens, start with a letter, end with a letter or digit, and not contain consecutive hyphens."
  }
}

variable "redis_container_app_name" {
  description = "Name of the dev-free Redis Container App."
  type        = string
  default     = "ca-redis-logisticshub-dev-free"

  validation {
    condition     = length(var.redis_container_app_name) <= 32 && can(regex("^[a-z]([a-z0-9-]*[a-z0-9])?$", var.redis_container_app_name)) && !strcontains(var.redis_container_app_name, "--")
    error_message = "redis_container_app_name must be no more than 32 characters, use only lowercase letters, digits, and hyphens, start with a letter, end with a letter or digit, and not contain consecutive hyphens."
  }
}

variable "gateway_container_app_name" {
  description = "Name of the dev-free Gateway Container App."
  type        = string
  default     = "ca-gateway-logisticshub-dev-free"

  validation {
    condition     = length(var.gateway_container_app_name) <= 32 && can(regex("^[a-z]([a-z0-9-]*[a-z0-9])?$", var.gateway_container_app_name)) && !strcontains(var.gateway_container_app_name, "--")
    error_message = "gateway_container_app_name must be no more than 32 characters, use only lowercase letters, digits, and hyphens, start with a letter, end with a letter or digit, and not contain consecutive hyphens."
  }
}

variable "companyservice_container_app_name" {
  description = "Name of the dev-free CompanyService Container App."
  type        = string
  default     = "ca-company-logisticshub-dev-free"

  validation {
    condition     = length(var.companyservice_container_app_name) <= 32 && can(regex("^[a-z]([a-z0-9-]*[a-z0-9])?$", var.companyservice_container_app_name)) && !strcontains(var.companyservice_container_app_name, "--")
    error_message = "companyservice_container_app_name must be no more than 32 characters, use only lowercase letters, digits, and hyphens, start with a letter, end with a letter or digit, and not contain consecutive hyphens."
  }
}

variable "inventoryservice_container_app_name" {
  description = "Name of the dev-free InventoryService Container App."
  type        = string
  default     = "ca-inv-logisticshub-dev-free"

  validation {
    condition     = length(var.inventoryservice_container_app_name) <= 32 && can(regex("^[a-z]([a-z0-9-]*[a-z0-9])?$", var.inventoryservice_container_app_name)) && !strcontains(var.inventoryservice_container_app_name, "--")
    error_message = "inventoryservice_container_app_name must be no more than 32 characters, use only lowercase letters, digits, and hyphens, start with a letter, end with a letter or digit, and not contain consecutive hyphens."
  }
}

variable "shipmentservice_container_app_name" {
  description = "Name of the dev-free ShipmentService Container App."
  type        = string
  default     = "ca-ship-logisticshub-dev-free"

  validation {
    condition     = length(var.shipmentservice_container_app_name) <= 32 && can(regex("^[a-z]([a-z0-9-]*[a-z0-9])?$", var.shipmentservice_container_app_name)) && !strcontains(var.shipmentservice_container_app_name, "--")
    error_message = "shipmentservice_container_app_name must be no more than 32 characters, use only lowercase letters, digits, and hyphens, start with a letter, end with a letter or digit, and not contain consecutive hyphens."
  }
}

variable "cacheworker_container_app_name" {
  description = "Name of the dev-free CacheWorker Container App."
  type        = string
  default     = "ca-cache-logisticshub-dev-free"

  validation {
    condition     = length(var.cacheworker_container_app_name) <= 32 && can(regex("^[a-z]([a-z0-9-]*[a-z0-9])?$", var.cacheworker_container_app_name)) && !strcontains(var.cacheworker_container_app_name, "--")
    error_message = "cacheworker_container_app_name must be no more than 32 characters, use only lowercase letters, digits, and hyphens, start with a letter, end with a letter or digit, and not contain consecutive hyphens."
  }
}

variable "frontend_storage_account_name" {
  description = "Globally unique Storage Account name for the Angular static website."
  type        = string

  validation {
    condition     = can(regex("^[a-z0-9]{3,24}$", var.frontend_storage_account_name))
    error_message = "frontend_storage_account_name must be 3-24 lowercase letters or numbers."
  }
}

variable "rabbitmq_username" {
  description = "Non-guest RabbitMQ application username for the dev-free broker."
  type        = string
  default     = "logisticshub"

  validation {
    condition     = length(trimspace(var.rabbitmq_username)) > 0 && lower(var.rabbitmq_username) != "guest"
    error_message = "rabbitmq_username must be non-empty and must not be guest."
  }
}

variable "rabbitmq_password" {
  description = "RabbitMQ application password for the dev-free broker."
  type        = string
  sensitive   = true
  nullable    = false

  validation {
    condition     = length(trimspace(var.rabbitmq_password)) > 0
    error_message = "rabbitmq_password must be non-empty."
  }
}

variable "redis_password" {
  description = "Redis password for the dev-free cache."
  type        = string
  sensitive   = true
  nullable    = false

  validation {
    condition     = length(trimspace(var.redis_password)) > 0
    error_message = "redis_password must be non-empty."
  }
}

variable "tags" {
  description = "Common tags applied to dev-free resources."
  type        = map(string)
  default = {
    application = "logisticshub"
    environment = "dev-free"
    managed_by  = "terraform"
  }
}
