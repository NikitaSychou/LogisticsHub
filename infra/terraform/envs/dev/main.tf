data "azurerm_client_config" "current" {}

locals {
  database_names = {
    company   = "CompanyDb"
    inventory = "InventoryDb"
    shipment  = "ShipmentDb"
  }
}

resource "azurerm_resource_group" "main" {
  name     = var.resource_group_name
  location = var.location
  tags     = var.tags
}

resource "azurerm_log_analytics_workspace" "main" {
  name                = var.log_analytics_workspace_name
  resource_group_name = azurerm_resource_group.main.name
  location            = azurerm_resource_group.main.location
  sku                 = "PerGB2018"
  retention_in_days   = 30
  tags                = var.tags
}

resource "azurerm_container_registry" "main" {
  name                = var.acr_name
  resource_group_name = azurerm_resource_group.main.name
  location            = azurerm_resource_group.main.location
  sku                 = var.container_registry_sku
  admin_enabled       = false
  tags                = var.tags
}

resource "azurerm_kubernetes_cluster" "main" {
  name                = var.aks_name
  resource_group_name = azurerm_resource_group.main.name
  location            = azurerm_resource_group.main.location
  dns_prefix          = var.aks_dns_prefix
  kubernetes_version  = var.aks_kubernetes_version

  default_node_pool {
    name       = "system"
    node_count = var.aks_node_count
    vm_size    = var.aks_node_vm_size
  }

  identity {
    type = "SystemAssigned"
  }

  oms_agent {
    log_analytics_workspace_id = azurerm_log_analytics_workspace.main.id
  }

  role_based_access_control_enabled = true
  local_account_disabled            = false
  tags                              = var.tags
}

resource "azurerm_mssql_server" "main" {
  name                          = var.sql_server_name
  resource_group_name           = azurerm_resource_group.main.name
  location                      = azurerm_resource_group.main.location
  version                       = "12.0"
  administrator_login           = var.sql_admin_login
  administrator_login_password  = var.sql_admin_password
  minimum_tls_version           = "1.2"
  public_network_access_enabled = true
  tags                          = var.tags
}

resource "azurerm_mssql_database" "databases" {
  for_each  = local.database_names
  name      = each.value
  server_id = azurerm_mssql_server.main.id
  sku_name  = var.sql_database_sku_name
  tags      = var.tags
}

resource "azurerm_redis_cache" "main" {
  name                = var.redis_name
  resource_group_name = azurerm_resource_group.main.name
  location            = azurerm_resource_group.main.location
  capacity            = var.redis_capacity
  family              = var.redis_family
  sku_name            = var.redis_sku_name
  minimum_tls_version = "1.2"
  tags                = var.tags
}

resource "azurerm_key_vault" "main" {
  name                       = var.key_vault_name
  resource_group_name        = azurerm_resource_group.main.name
  location                   = azurerm_resource_group.main.location
  tenant_id                  = data.azurerm_client_config.current.tenant_id
  sku_name                   = "standard"
  soft_delete_retention_days = 7
  purge_protection_enabled   = false
  enable_rbac_authorization  = true
  tags                       = var.tags
}

resource "azurerm_storage_account" "frontend" {
  name                              = var.frontend_storage_account_name
  resource_group_name               = azurerm_resource_group.main.name
  location                          = azurerm_resource_group.main.location
  account_tier                      = "Standard"
  account_replication_type          = "LRS"
  account_kind                      = "StorageV2"
  min_tls_version                   = "TLS1_2"
  allow_nested_items_to_be_public   = false
  shared_access_key_enabled         = true
  infrastructure_encryption_enabled = true
  tags                              = var.tags
}

resource "azurerm_storage_account_static_website" "frontend" {
  storage_account_id = azurerm_storage_account.frontend.id
  index_document     = "index.html"
  error_404_document = "index.html"
}
