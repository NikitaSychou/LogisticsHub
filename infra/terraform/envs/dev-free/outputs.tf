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

output "sql_server_name" {
  description = "Azure SQL logical server name."
  value       = azurerm_mssql_server.main.name
}

output "sql_server_fqdn" {
  description = "Azure SQL logical server fully qualified domain name."
  value       = azurerm_mssql_server.main.fully_qualified_domain_name
}

output "sql_database_names" {
  description = "Azure SQL database names for dev-free services."
  value       = local.sql_database_names
}

output "sql_database_ids" {
  description = "Azure SQL database resource IDs."
  value       = { for key, database in azapi_resource.sql_database : key => database.id }
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

output "gateway_container_app_name" {
  description = "Name of the dev-free Gateway Container App."
  value       = azurerm_container_app.gateway.name
}

output "gateway_fqdn" {
  description = "Public FQDN for the dev-free Gateway Container App."
  value       = azurerm_container_app.gateway.ingress[0].fqdn
}

output "gateway_url" {
  description = "Public HTTPS URL for the dev-free Gateway Container App."
  value       = "https://${azurerm_container_app.gateway.ingress[0].fqdn}"
}

output "gateway_port" {
  description = "Internal HTTP port for Gateway."
  value       = local.container_app_http_port
}

output "companyservice_container_app_name" {
  description = "Name of the dev-free CompanyService Container App."
  value       = azurerm_container_app.companyservice.name
}

output "companyservice_internal_fqdn" {
  description = "Internal FQDN for the dev-free CompanyService Container App."
  value       = azurerm_container_app.companyservice.ingress[0].fqdn
}

output "companyservice_internal_url" {
  description = "Internal HTTP URL for the dev-free CompanyService Container App."
  value       = "http://${azurerm_container_app.companyservice.ingress[0].fqdn}"
}

output "companyservice_port" {
  description = "Internal HTTP port for CompanyService."
  value       = local.container_app_http_port
}

output "inventoryservice_container_app_name" {
  description = "Name of the dev-free InventoryService Container App."
  value       = azurerm_container_app.inventoryservice.name
}

output "inventoryservice_internal_fqdn" {
  description = "Internal FQDN for the dev-free InventoryService Container App."
  value       = azurerm_container_app.inventoryservice.ingress[0].fqdn
}

output "inventoryservice_internal_url" {
  description = "Internal HTTP URL for the dev-free InventoryService Container App."
  value       = "http://${azurerm_container_app.inventoryservice.ingress[0].fqdn}"
}

output "inventoryservice_port" {
  description = "Internal HTTP port for InventoryService."
  value       = local.container_app_http_port
}

output "shipmentservice_container_app_name" {
  description = "Name of the dev-free ShipmentService Container App."
  value       = azurerm_container_app.shipmentservice.name
}

output "shipmentservice_internal_fqdn" {
  description = "Internal FQDN for the dev-free ShipmentService Container App."
  value       = azurerm_container_app.shipmentservice.ingress[0].fqdn
}

output "shipmentservice_internal_url" {
  description = "Internal HTTP URL for the dev-free ShipmentService Container App."
  value       = "http://${azurerm_container_app.shipmentservice.ingress[0].fqdn}"
}

output "shipmentservice_port" {
  description = "Internal HTTP port for ShipmentService."
  value       = local.container_app_http_port
}

output "cacheworker_container_app_name" {
  description = "Name of the dev-free CacheWorker Container App."
  value       = azurerm_container_app.cacheworker.name
}

output "frontend_storage_account_name" {
  description = "Frontend Storage Account name."
  value       = azurerm_storage_account.frontend.name
}

output "frontend_static_website_endpoint" {
  description = "Static website endpoint for future Angular hosting."
  value       = azurerm_storage_account.frontend.primary_web_endpoint
}
