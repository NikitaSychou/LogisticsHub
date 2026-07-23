# LogisticsHub Dev-Free Terraform Environment

This root defines the simplified low-cost Azure foundation for the dev-free target. New or changed resources are not created until `terraform apply` is run manually after review. The production root under `envs/prod` remains unchanged.

## Resources

- Resource group for the dev-free foundation.
- Azure Container Apps managed environment using the default Consumption workload profile.
- Internal RabbitMQ Container App for dev-only messaging.
- Internal Redis Container App for dev-only cache and runtime dependency use.
- Storage Account for future Angular static website hosting.
- Storage Static Website configuration with `index.html` as both the index and error document.

This root does not deploy application containers, Gateway, backend services, CacheWorker, Azure SQL, ACR, Key Vault, managed identities, role assignments, custom domains, Front Door, CDN, GitHub Actions, Dockerfiles, Kubernetes manifests, or application configuration.

## Cost And Logging

The Container Apps environment is configured for the default Consumption model. No dedicated workload profile, custom VNet, infrastructure subnet, internal load balancer, private endpoint, zone redundancy, Log Analytics workspace, or Application Insights resource is created.

`logs_destination` is intentionally omitted so the environment does not require a Log Analytics workspace in this foundation. This keeps the initial footprint small, but the design targets free-tier or trial-credit usage rather than guaranteeing zero consumption under all workloads.

## Dev Runtime Dependencies

RabbitMQ and Redis run as dev-only Container Apps in the shared Consumption environment. Both use one replica, internal TCP ingress only, and no persistent volumes or high-availability configuration. RabbitMQ listens on port `5672`; the management UI on `15672` is not exposed and the image does not include the management plugin. Redis listens on port `6379`, requires authentication, and disables AOF and RDB snapshot persistence.

Broker and cache data is ephemeral: restarts, revision replacement, stopping, or destroying dev-free can lose temporary data. Passwords come from ignored local variables and are stored as Container Apps secrets; sensitive Terraform values are also stored in the protected remote state even when marked sensitive. These runtime dependencies are not part of the preserved production AKS architecture.

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

- A globally unique `frontend_storage_account_name`.
- Real `rabbitmq_password` and `redis_password` values.

Do not commit subscription IDs, tenant IDs, credentials, access keys, SAS tokens, passwords, connection strings, or local operator values.

## Validation

Run local validation with:

```powershell
terraform fmt -check
terraform init -backend=false -input=false
terraform validate
```

Do not run `terraform apply` until a focused plan has been reviewed.
