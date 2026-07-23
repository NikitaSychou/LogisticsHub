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
