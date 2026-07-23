output "resource_group_name" {
  description = "Dev-free resource group name."
  value       = azurerm_resource_group.main.name
}

output "container_app_environment_name" {
  description = "Container Apps managed environment name."
  value       = azurerm_container_app_environment.main.name
}

output "container_app_environment_id" {
  description = "Container Apps managed environment resource ID."
  value       = azurerm_container_app_environment.main.id
}

output "container_app_environment_default_domain" {
  description = "Default domain for apps in the Container Apps managed environment."
  value       = azurerm_container_app_environment.main.default_domain
}

output "frontend_storage_account_name" {
  description = "Frontend Storage Account name."
  value       = azurerm_storage_account.frontend.name
}

output "frontend_static_website_endpoint" {
  description = "Static website endpoint for future Angular hosting."
  value       = azurerm_storage_account.frontend.primary_web_endpoint
}