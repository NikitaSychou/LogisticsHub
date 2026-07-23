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

output "rabbitmq_container_app_name" {
  description = "Name of the dev-free RabbitMQ Container App."
  value       = azurerm_container_app.rabbitmq.name
}

output "rabbitmq_internal_fqdn" {
  description = "Internal FQDN for the dev-free RabbitMQ Container App."
  value       = azurerm_container_app.rabbitmq.ingress[0].fqdn
}

output "rabbitmq_port" {
  description = "Internal TCP port for RabbitMQ."
  value       = local.rabbitmq_port
}

output "redis_container_app_name" {
  description = "Name of the dev-free Redis Container App."
  value       = azurerm_container_app.redis.name
}

output "redis_internal_fqdn" {
  description = "Internal FQDN for the dev-free Redis Container App."
  value       = azurerm_container_app.redis.ingress[0].fqdn
}

output "redis_port" {
  description = "Internal TCP port for Redis."
  value       = local.redis_port
}

output "frontend_storage_account_name" {
  description = "Frontend Storage Account name."
  value       = azurerm_storage_account.frontend.name
}

output "frontend_static_website_endpoint" {
  description = "Static website endpoint for future Angular hosting."
  value       = azurerm_storage_account.frontend.primary_web_endpoint
}
