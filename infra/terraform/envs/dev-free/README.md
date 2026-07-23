# LogisticsHub Dev-Free Terraform Environment

This root defines the first simplified low-cost Azure foundation for the dev-free target. No Azure resources exist until `terraform apply` is run manually after review. The production root under `envs/prod` remains unchanged.

## Resources

- Resource group for the dev-free foundation.
- Azure Container Apps managed environment using the default Consumption workload profile.
- Storage Account for future Angular static website hosting.
- Storage Static Website configuration with `index.html` as both the index and error document.

This PR does not deploy Container Apps applications, application containers, Gateway, backend services, CacheWorker, RabbitMQ, Redis, Azure SQL, ACR, Key Vault, managed identities, role assignments, secrets, custom domains, Front Door, CDN, GitHub Actions, Dockerfiles, Kubernetes manifests, or application configuration.

## Cost And Logging

The Container Apps environment is configured for the default Consumption model. No dedicated workload profile, custom VNet, infrastructure subnet, internal load balancer, private endpoint, zone redundancy, Log Analytics workspace, or Application Insights resource is created.

`logs_destination` is intentionally omitted so the environment does not require a Log Analytics workspace in this foundation. This keeps the initial footprint small, but the design targets free-tier or trial-credit usage rather than guaranteeing zero consumption under all workloads.

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

Copy `dev-free.tfvars.example` to an uncommitted `.tfvars` file and replace the `<unique>` placeholder in the Storage Account name.

Do not commit subscription IDs, tenant IDs, credentials, access keys, SAS tokens, passwords, connection strings, or local operator values.

## Validation

Run local validation with:

```powershell
terraform fmt -check
terraform init -backend=false -input=false
terraform validate
```

Do not run `terraform apply` until a focused plan has been reviewed.