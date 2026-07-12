variable "location" {
  description = "Azure region for Terraform state resources. Compare westeurope and northeurope before applying."
  type        = string
  default     = "westeurope"
}

variable "resource_group_name" {
  description = "Resource group for Terraform remote state."
  type        = string
}

variable "storage_account_name" {
  description = "Globally unique storage account name for Terraform remote state. Use lowercase letters and numbers only."
  type        = string

  validation {
    condition     = can(regex("^[a-z0-9]{3,24}$", var.storage_account_name))
    error_message = "Storage account names must be 3-24 lowercase letters or numbers."
  }
}

variable "container_name" {
  description = "Blob container name for Terraform state files."
  type        = string
  default     = "tfstate"
}

variable "tags" {
  description = "Tags applied to bootstrap resources."
  type        = map(string)
  default = {
    application = "logisticshub"
    environment = "bootstrap"
    managed_by  = "terraform"
  }
}
