# LogisticsHub Dev-Free Terraform Environment

This root defines the simplified low-cost Azure foundation for the dev-free target. New or changed resources are not created until `terraform apply` is run manually after review. The production root under `envs/prod` remains unchanged.

## Resources

- Resource group for the dev-free foundation.
- Azure Container Apps managed environment using the default Consumption workload profile.
- Internal RabbitMQ Container App for dev-only messaging.
- Internal Redis Container App for dev-only cache and runtime dependency use.
- Internal CompanyService, InventoryService, ShipmentService, and CacheWorker Container Apps.
- Storage Account for future Angular static website hosting.
- Storage Static Website configuration with `index.html` as both the index and error document.

This root still does not deploy Angular application files, SQL schemas, seed data, ACR, Key Vault, managed identities, role assignments, custom domains, Front Door, CDN, GitHub Actions, Dockerfiles, Kubernetes manifests, private endpoints, or production networking.

## Azure SQL Databases

The dev-free SQL foundation creates one shared logical Azure SQL server in Sweden Central and three empty database-per-service databases: CompanyService owns `CompanyDb`, InventoryService owns `InventoryDb`, and ShipmentService owns `ShipmentDb`. CacheWorker also requires `CompanyDb` access for cache warmup; Gateway has no direct SQL access and receives no SQL, Redis, RabbitMQ, or Container Apps secret values.

The databases use the Azure SQL free offer with `BillOverUsage` exhaustion behavior so dev-free E2E flows can continue after the monthly free compute allowance is exhausted. Schemas and seed data remain manual SQL only; EF migrations remain prohibited.

Because the current Container Apps environment has no VNet integration, SQL public network access and the `0.0.0.0` Azure-services firewall rule are explicit dev-free-only compromises. Authentication is the main security boundary. Local SQL deployment should use a temporary operator firewall rule created and removed outside this Terraform PR; production networking and private endpoint strategy remain unchanged.

SQL administrator passwords come from ignored local variables and are stored in protected Terraform remote state when applied. Connection strings and passwords are not exposed through Terraform outputs.

## Internal Services

CompanyService, InventoryService, and ShipmentService run as internal HTTP Container Apps on port `8080` with no public ingress. CacheWorker runs as a worker Container App with no ingress, no exposed port, and no HTTP probes; its health is represented by process lifecycle and Container Apps revision state.

Gateway, CompanyService, InventoryService, and ShipmentService use the Consumption workload profile, single revision mode, zero minimum replicas, and one maximum replica. Their HTTP ingress remains enabled, so incoming frontend or internal HTTP requests provide the HTTP wake-up path. Cold starts can include Container Apps startup plus Azure SQL serverless resume latency.

InventoryService and ShipmentService also have RabbitMQ `QueueLength` scale rules for their consumer queues. InventoryService wakes for `inventory.stock-reservation.requested`; ShipmentService wakes for `shipment.stock-reservation.reserved` and `shipment.stock-reservation.failed`. The scale rules use AMQP against the internal RabbitMQ Container App, keep credentials out of rule metadata, and read the password from the existing `rabbitmq-password` Container Apps secret.

InventoryService and ShipmentService each have a dev-free-only cron safety window from `0 */3 * * *` through `10 */3 * * *` in `Etc/UTC`, with desired replicas set to `1`. This gives delayed outbox retries a short processing window every three hours while leaving enough idle time for Azure SQL serverless databases to auto-pause. The HTTP cold-start behavior still applies outside that window, and dev-free keeps ShipmentService's temporary 60-second CompanyService call timeout to tolerate CompanyService and SQL cold starts.

CacheWorker, RabbitMQ, and Redis intentionally keep one minimum replica. CacheWorker scheduling or job conversion, RabbitMQ TCP dependency scale-to-zero, and Redis scale-to-zero or managed-cache replacement remain deferred follow-up optimizations.

The API containers use `/health/live` for startup and liveness probes and `/health/ready` for readiness probes. Readiness checks continue to include the service dependencies configured by the applications, such as SQL and RabbitMQ.

Container image references are built from a required immutable full Git commit SHA in `container_image_tag`; `latest` is not used by Terraform. The GHCR packages must be Public before planning or deployment so Azure Container Apps can pull them without registry credentials.

Angular remains deferred. Gateway CORS is configured with the Storage Static Website origin derived from Terraform output; the Angular deployment still remains future work.

## Cost And Logging

The Container Apps environment is configured for the default Consumption model. No dedicated workload profile, custom VNet, infrastructure subnet, internal load balancer, private endpoint, zone redundancy, Log Analytics workspace, or Application Insights resource is created.

`logs_destination` is intentionally omitted so the environment does not require a Log Analytics workspace in this foundation. This keeps the initial footprint small, but the design targets free-tier or trial-credit usage rather than guaranteeing zero consumption under all workloads.

## Dev Runtime Dependencies

RabbitMQ and Redis run as dev-only Container Apps in the shared Consumption environment. Both use one replica, internal TCP ingress only, and no persistent volumes or high-availability configuration. RabbitMQ listens on port `5672`; the management UI on `15672` is not exposed and the image does not include the management plugin. Redis listens on port `6379`, requires authentication, and disables AOF and RDB snapshot persistence.

RabbitMQ imports `rabbitmq-definitions.json` during broker boot using RabbitMQ's local-filesystem definitions import settings. The tracked definitions file contains no users, passwords, or permissions; it bootstraps only the durable exchange, dead-letter exchange, consumer queues, dead-letter queues, and bindings required by the dev-free message flow. Consumer declarations remain compatible and idempotent with the bootstrapped topology.

At startup, the RabbitMQ container combines the tracked topology with a runtime-only user entry and permissions for `/`. The password hash is generated from the existing secret-backed RabbitMQ password before broker startup and is written only to the container's ephemeral runtime definitions file. Broker and cache data is ephemeral: restarts, revision replacement, stopping, or destroying dev-free can lose temporary data. Passwords come from ignored local variables and are stored as Container Apps secrets; sensitive Terraform values are also stored in the protected remote state even when marked sensitive. These runtime dependencies are not part of the preserved production AKS architecture.

## Remote State

`backend.tf` is tracked and contains only the partial `azurerm` backend declaration. Keep environment-specific backend settings in a local ignored `backend.hcl` file.

Copy `backend.tf.example` to `backend.hcl` after bootstrap and keep the state key separate from other environments:

- `logisticshub/dev-free.tfstate`

Initialize with Azure AD authentication:

```powershell
terraform init -reconfigure -backend-config="backend.hcl"
```

Backend initialization does not require `terraform apply`.

## Required Local Values

Copy `dev-free.tfvars.example` to an uncommitted `.tfvars` file and replace placeholders with local values:

- A full immutable master commit SHA in `container_image_tag` for the public GHCR images.
- Microsoft Entra API values: `azure_ad_tenant_id`, `azure_ad_client_id`, `azure_ad_audience`, and `azure_ad_required_scope`.
- A globally unique `frontend_storage_account_name`.
- Real `sql_administrator_login_password`, `rabbitmq_password`, and `redis_password` values.

Do not commit subscription IDs, tenant IDs, credentials, access keys, SAS tokens, passwords, connection strings, or local operator values.

## Plan And Apply

Terraform apply for dev-free must run from `master` only, after the focused plan has been reviewed.

```powershell
terraform fmt -check
terraform validate
terraform plan -var-file="dev-free.tfvars" -no-color -input=false
terraform apply -var-file="dev-free.tfvars" -input=false
```

Do not run `terraform apply` from feature branches.

## Post-Apply Checks

These checks do not print secrets:

```powershell
terraform output gateway_url
terraform output companyservice_internal_url
terraform output inventoryservice_internal_url
terraform output shipmentservice_internal_url
terraform output cacheworker_container_app_name
az containerapp show --resource-group rg-logisticshub-dev-free --name ca-gateway-logisticshub-dev-free --query properties.configuration.ingress.fqdn -o tsv
az containerapp show --resource-group rg-logisticshub-dev-free --name ca-company-logisticshub-dev-free --query properties.provisioningState -o tsv
az containerapp show --resource-group rg-logisticshub-dev-free --name ca-inv-logisticshub-dev-free --query properties.provisioningState -o tsv
az containerapp show --resource-group rg-logisticshub-dev-free --name ca-ship-logisticshub-dev-free --query properties.provisioningState -o tsv
az containerapp show --resource-group rg-logisticshub-dev-free --name ca-cache-logisticshub-dev-free --query properties.provisioningState -o tsv
```

Do not print Container Apps secrets or Terraform sensitive values during verification.

## Validation

Run local validation with:

```powershell
terraform fmt -check
terraform init -backend=false -input=false
terraform validate
```

Do not run `terraform apply` until a focused plan has been reviewed.