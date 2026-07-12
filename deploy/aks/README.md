# AKS Backend Deployment Skeleton

This folder is a minimal Kubernetes skeleton for the LogisticsHub backend. It is intentionally not a complete production deployment.

## Scope

- Gateway is the only backend workload intended for public ingress.
- CompanyService, InventoryService, and ShipmentService are internal `ClusterIP` services.
- CacheWorker runs without a Kubernetes Service or ingress.
- Angular is hosted separately through Azure Storage Static Website and Azure Front Door/CDN.
- Azure SQL Database and Azure Cache for Redis are managed Azure services.
- RabbitMQ production setup is a separate follow-up; the manifests only reserve configuration keys for it.

See [RabbitMQ AKS production strategy](../../docs/rabbitmq-aks-production-strategy.md) for the planned broker approach.

## Before Real Deployment

Replace placeholder image names in `kustomization.yaml` with ACR image names and immutable tags.

Create the referenced secret outside source control. Do not commit real secret values.

```powershell
kubectl create secret generic logisticshub-backend-secrets `
  --from-literal=CompanyDbConnectionString="<azure-sql-company-connection-string>" `
  --from-literal=InventoryDbConnectionString="<azure-sql-inventory-connection-string>" `
  --from-literal=ShipmentDbConnectionString="<azure-sql-shipment-connection-string>" `
  --from-literal=RedisConnectionString="<azure-cache-for-redis-connection-string>" `
  --from-literal=RabbitMqHostName="<rabbitmq-host>" `
  --from-literal=RabbitMqPort="<rabbitmq-port>" `
  --from-literal=RabbitMqUserName="<rabbitmq-user>" `
  --from-literal=RabbitMqPassword="<rabbitmq-password>" `
  --from-literal=AzureAdInstance="<entra-instance>" `
  --from-literal=AzureAdTenantId="<tenant-id>" `
  --from-literal=AzureAdClientId="<api-app-client-id>" `
  --from-literal=AzureAdAudience="<api-audience>" `
  --from-literal=AzureAdRequiredScope="<api-scope>"
```

Review `configmap.yaml` before deployment. It contains non-secret service discovery values and can be adjusted for the final Kubernetes service names.

## Ingress

This skeleton does not include an ingress resource. A later deployment-specific PR should choose and configure the AKS ingress path for Gateway, including host names, TLS, certificate management, and any Azure Application Gateway or NGINX controller details.

Do not expose CompanyService, InventoryService, ShipmentService, or CacheWorker directly.

## Validation

Render the skeleton locally before applying:

```powershell
kubectl kustomize deploy/aks
```

The rendered output still requires real images, real secrets, and production infrastructure before it can run.
