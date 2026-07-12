variable "location" {
  description = "Azure region. Default candidate is westeurope; compare westeurope and northeurope costs before applying."
  type        = string
  default     = "westeurope"
}

variable "resource_group_name" {
  description = "Resource group for the LogisticsHub dev foundation."
  type        = string
}

variable "acr_name" {
  description = "Globally unique Azure Container Registry name. Use letters and numbers only."
  type        = string

  validation {
    condition     = can(regex("^[a-zA-Z0-9]{5,50}$", var.acr_name))
    error_message = "ACR names must be 5-50 alphanumeric characters."
  }
}

variable "aks_name" {
  description = "AKS cluster name."
  type        = string
}

variable "aks_dns_prefix" {
  description = "AKS DNS prefix."
  type        = string
}

variable "aks_kubernetes_version" {
  description = "Optional AKS Kubernetes version. Leave null to use the provider default."
  type        = string
  default     = null
}

variable "aks_node_vm_size" {
  description = "Dev AKS system node VM size. Compare cost before applying."
  type        = string
  default     = "Standard_B2s"
}

variable "aks_node_count" {
  description = "Dev AKS system node count."
  type        = number
  default     = 1
}

variable "log_analytics_workspace_name" {
  description = "Log Analytics workspace name."
  type        = string
}

variable "container_registry_sku" {
  description = "ACR SKU."
  type        = string
  default     = "Basic"
}

variable "sql_server_name" {
  description = "Globally unique Azure SQL logical server name."
  type        = string
}

variable "sql_admin_login" {
  description = "Azure SQL administrator login name. Do not use a personal account name."
  type        = string
}

variable "sql_admin_password" {
  description = "Azure SQL administrator password. Supply locally through TF_VAR_sql_admin_password or an uncommitted tfvars file."
  type        = string
  sensitive   = true
}

variable "sql_database_sku_name" {
  description = "Dev Azure SQL database SKU."
  type        = string
  default     = "Basic"
}

variable "redis_name" {
  description = "Globally unique Azure Cache for Redis name."
  type        = string
}

variable "redis_capacity" {
  description = "Redis cache capacity for the selected family."
  type        = number
  default     = 0
}

variable "redis_family" {
  description = "Redis SKU family."
  type        = string
  default     = "C"
}

variable "redis_sku_name" {
  description = "Redis SKU name."
  type        = string
  default     = "Basic"
}

variable "key_vault_name" {
  description = "Globally unique Key Vault name."
  type        = string
}

variable "frontend_storage_account_name" {
  description = "Globally unique storage account name for future Angular static website hosting."
  type        = string

  validation {
    condition     = can(regex("^[a-z0-9]{3,24}$", var.frontend_storage_account_name))
    error_message = "Storage account names must be 3-24 lowercase letters or numbers."
  }
}

variable "tags" {
  description = "Tags applied to dev foundation resources."
  type        = map(string)
  default = {
    application = "logisticshub"
    environment = "dev"
    managed_by  = "terraform"
  }
}
