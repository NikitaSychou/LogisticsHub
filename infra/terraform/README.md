# LogisticsHub Terraform Foundation

This folder contains the Terraform foundation for future LogisticsHub Azure deployment work. It is intentionally a foundation only: do not run `terraform apply` from automation or tooling until the values, costs, and security model have been reviewed.

## Layout

```text
infra/terraform/
  bootstrap/   # creates remote-state resource group, storage account, and blob container
  envs/dev/    # dev Azure foundation using remote state after bootstrap
```

## Region Decision

Terraform uses a `location` variable. The dev examples default to `westeurope` as the candidate region.

Before any real `terraform apply`, compare `westeurope` and `northeurope` in the Azure Pricing Calculator for the selected AKS VM size, Azure SQL SKU, Redis SKU, and Storage Account settings. Use `northeurope` if it is cheaper for the final selected resource set.

Do not hardcode a region in resource definitions; pass it through variables.

## Deployment Flow

1. Review and run the bootstrap layer manually to create remote Terraform state storage.
2. Copy `envs/dev/backend.tf.example` to `envs/dev/backend.tf` and fill in the bootstrap outputs.
3. Copy `envs/dev/dev.tfvars.example` to a local `.tfvars` file and fill in environment-specific values.
4. Run `terraform init`, `terraform plan`, and only then a reviewed manual `terraform apply`.

Codex must not run `terraform apply` or create Azure resources.

## Azure Resource Providers

LogisticsHub registers required Azure Resource Providers manually before Terraform plan/apply. AzureRM automatic Resource Provider registration is intentionally disabled with `resource_provider_registrations = "none"` so Terraform does not try to register unrelated providers.

Required providers for this foundation are:

- `Microsoft.ContainerService`
- `Microsoft.ContainerRegistry`
- `Microsoft.Sql`
- `Microsoft.Cache`
- `Microsoft.KeyVault`
- `Microsoft.Storage`
- `Microsoft.OperationalInsights`
- `Microsoft.Compute`
- `Microsoft.Network`
- `Microsoft.ManagedIdentity`

## Relation To Existing Deployment Work

- `deploy/aks` contains placeholder Kubernetes manifests that can later target the AKS cluster from this Terraform.
- [Azure SQL manual deployment runbook](../../docs/azure-sql-manual-deploy-runbook.md) remains the database schema process. EF migrations are not used.
- [AKS Key Vault and secrets strategy](../../docs/aks-key-vault-secrets-strategy.md) describes future Key Vault, Workload Identity, and Secrets Store CSI Driver wiring. This Terraform does not install CSI or create real secrets.
- [RabbitMQ AKS production strategy](../../docs/rabbitmq-aks-production-strategy.md) remains a follow-up. This Terraform does not install RabbitMQ or the RabbitMQ Cluster Operator.
- [Angular Azure Static Hosting](../../docs/frontend-azure-static-hosting.md) documents the frontend model. This Terraform creates only a storage account suitable for future static website hosting; Front Door/CDN is a separate follow-up.

## Cost And Secret Warnings

AKS nodes, Azure SQL Database, Redis, Container Registry, Log Analytics, Key Vault operations, and Storage may incur cost even in dev. Review SKUs before applying.

Terraform state and plan files can contain sensitive values, including values passed through sensitive variables such as the Azure SQL administrator password. Treat the state storage account and blob container as sensitive assets. Remote state access must be tightly restricted.

Use Microsoft Entra ID / Azure AD and Azure RBAC for the Azure Storage backend where feasible. Access keys and SAS tokens are not the preferred approach for new workloads. The Terraform operator or automation principal should receive only the minimum blob data permissions needed for the state container, such as Storage Blob Data Contributor scoped to that container where feasible.

Do not commit:

- Azure subscription IDs, tenant IDs, object IDs, client IDs, or production URLs;
- SQL admin passwords, connection strings, Redis keys, RabbitMQ credentials, or client secrets;
- `.tfstate`, `.tfplan`, `.terraform/`, crash logs, or local `.tfvars` files.

The root `.gitignore` excludes Terraform state, plans, crash logs, and local tfvars while keeping `*.tfvars.example` files trackable.

Terraform provider lock files should be reviewed when provider versions change. Commit `.terraform.lock.hcl` only after the selected provider versions are accepted.
