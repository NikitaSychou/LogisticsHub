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

variable "frontend_storage_account_name" {
  description = "Globally unique Storage Account name for the Angular static website."
  type        = string

  validation {
    condition     = can(regex("^[a-z0-9]{3,24}$", var.frontend_storage_account_name))
    error_message = "frontend_storage_account_name must be 3-24 lowercase letters or numbers."
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