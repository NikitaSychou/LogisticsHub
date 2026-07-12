output "resource_group_name" {
  description = "Dev resource group name."
  value       = azurerm_resource_group.main.name
}

output "acr_login_server" {
  description = "ACR login server for future backend images."
  value       = azurerm_container_registry.main.login_server
}

output "aks_cluster_name" {
  description = "AKS cluster name."
  value       = azurerm_kubernetes_cluster.main.name
}

output "aks_kubelet_identity_object_id" {
  description = "AKS kubelet identity object ID for future ACR pull role assignment hardening."
  value       = azurerm_kubernetes_cluster.main.kubelet_identity[0].object_id
}

output "sql_server_fqdn" {
  description = "Azure SQL server FQDN."
  value       = azurerm_mssql_server.main.fully_qualified_domain_name
}

output "sql_database_names" {
  description = "Service database names."
  value       = [for database in azurerm_mssql_database.databases : database.name]
}

output "redis_hostname" {
  description = "Redis hostname. Access keys are intentionally not output."
  value       = azurerm_redis_cache.main.hostname
}

output "key_vault_uri" {
  description = "Key Vault URI for future secret wiring."
  value       = azurerm_key_vault.main.vault_uri
}

output "frontend_static_website_endpoint" {
  description = "Static website endpoint for future Angular hosting."
  value       = azurerm_storage_account.frontend.primary_web_endpoint
}
