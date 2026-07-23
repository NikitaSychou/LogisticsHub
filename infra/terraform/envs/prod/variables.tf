variable "location" {
  description = "Azure region. The production target requires final paid-subscription region review before apply."
  type        = string
  default     = "northeurope"
}

variable "resource_group_name" {
  description = "Resource group for the LogisticsHub dev foundation."
  type        = string
}

variable "virtual_network_name" {
  description = "Virtual network name for the LogisticsHub production environment."
  type        = string
}

variable "virtual_network_address_space" {
  description = "Address space for the dev virtual network. Keep it non-overlapping with AKS service and pod CIDRs."
  type        = list(string)
  default     = ["10.20.0.0/16"]

  validation {
    condition     = length(var.virtual_network_address_space) == 1 && alltrue([for cidr in var.virtual_network_address_space : can(cidrnetmask(cidr))])
    error_message = "virtual_network_address_space must contain exactly one valid IPv4 CIDR block."
  }
}

variable "aks_subnet_name" {
  description = "Dedicated AKS subnet name."
  type        = string
}

variable "aks_subnet_address_prefix" {
  description = "Address prefix for the dedicated AKS subnet. Keep it inside virtual_network_address_space."
  type        = string
  default     = "10.20.0.0/22"

  validation {
    condition     = can(cidrnetmask(var.aks_subnet_address_prefix))
    error_message = "aks_subnet_address_prefix must be a valid IPv4 CIDR block."
  }
}

variable "reserved_private_endpoint_subnet_address_prefix" {
  description = "Reserved future private-endpoint subnet prefix. This PR documents the reservation but does not create the subnet."
  type        = string
  default     = "10.20.4.0/24"

  validation {
    condition     = can(cidrnetmask(var.reserved_private_endpoint_subnet_address_prefix))
    error_message = "reserved_private_endpoint_subnet_address_prefix must be a valid IPv4 CIDR block."
  }
}

variable "aks_network_security_group_name" {
  description = "Network security group name associated with the AKS subnet."
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

variable "aks_service_cidr" {
  description = "AKS service CIDR. Keep it non-overlapping with the VNet and pod CIDR."
  type        = string
  default     = "10.30.0.0/16"

  validation {
    condition     = can(cidrnetmask(var.aks_service_cidr))
    error_message = "aks_service_cidr must be a valid IPv4 CIDR block."
  }
}

variable "aks_dns_service_ip" {
  description = "AKS DNS service IP. It must be the 10th host address in aks_service_cidr."
  type        = string
  default     = "10.30.0.10"

  validation {
    condition     = can(cidrnetmask("${var.aks_dns_service_ip}/32"))
    error_message = "aks_dns_service_ip must be a valid IPv4 address."
  }
}

variable "aks_pod_cidr" {
  description = "AKS pod CIDR for Azure CNI Overlay. Keep it non-overlapping with the VNet and service CIDR."
  type        = string
  default     = "10.40.0.0/16"

  validation {
    condition     = can(cidrnetmask(var.aks_pod_cidr))
    error_message = "aks_pod_cidr must be a valid IPv4 CIDR block."
  }
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
    environment = "prod"
    managed_by  = "terraform"
  }
}