# LogisticsHub Terraform

This folder contains Terraform for LogisticsHub Azure infrastructure. Do not run `terraform apply` from automation or tooling until the selected environment, values, costs, and security model have been reviewed.

## Layout

```text
infra/terraform/
  bootstrap/      # creates the shared remote-state resource group, storage account, and blob container
  envs/prod/      # full AKS-based paid production target, preserved from the original root
  envs/dev-free/  # simplified low-cost/free development target, intentionally empty in this PR
```

## Environment Matrix

| Environment | Purpose | State key | Current status |
| --- | --- | --- | --- |
| `prod` | Full paid production target with AKS, ACR, Azure SQL, Key Vault, Storage, observability, Workload Identity, AcrPull, and Key Vault CSI support. | `logisticshub/prod.tfstate` | Preserved but not currently apply-ready until remediation is complete. |
| `dev-free` | Simplified low-cost/free target. Azure Container Apps Consumption is planned instead of AKS; temporary RabbitMQ and Redis containers are planned; Azure SQL free-offer databases will be evaluated separately; Storage Static Website remains planned for frontend hosting. | `logisticshub/dev-free.tfstate` | Root exists but declares no Azure resources yet. |

Use separate root directories and separate backend state keys. Do not use Terraform CLI workspaces to switch LogisticsHub environments.

The previous partial application deployment was destroyed and the application Terraform state is empty, so no Terraform state migration is required for this environment split.

## Backend Flow

1. Review and run the bootstrap layer manually to create the shared remote Terraform state storage.
2. In the selected environment directory, copy `backend.tf.example` to a local ignored `backend.hcl`.
3. Fill in the bootstrap resource group, storage account, and container values.
4. Keep the environment-specific state key from the example.
5. Run `terraform init -reconfigure -backend-config="backend.hcl"`.
6. Copy the environment tfvars example, if one exists, to a local ignored `.tfvars` file and fill in reviewed values.
7. Run `terraform plan`, and only then a reviewed manual `terraform apply`.

Codex must not run `terraform apply`, `terraform destroy`, migrate state, or create Azure resources.

The backend uses Microsoft Entra ID / Azure AD-backed storage authentication with `use_azuread_auth = true`. Do not use access keys or SAS tokens for new remote state setup unless a reviewed exception is required. The Terraform operator or automation principal needs least-privilege blob data access to the state container, such as Storage Blob Data Contributor scoped to the container where feasible.

The AzureRM provider is configured with `storage_use_azuread = true` so Storage data-plane operations use Azure AD instead of storage account keys. This is required for the Terraform state storage account because shared key access is disabled.

## Production Target

`envs/prod` preserves the complete AKS-based architecture:

- explicit VNet, dedicated AKS subnet, NSG association, and Azure CNI Overlay;
- AKS default subnet outbound access disabled with Standard Load Balancer egress;
- AKS OIDC issuer and Microsoft Entra Workload Identity;
- AKS kubelet AcrPull access to ACR without ACR admin credentials or image pull secrets;
- Key Vault Secrets Store CSI provider support with mounted secret rotation;
- Azure SQL, ACR, Key Vault, Storage Static Website, and Log Analytics foundation resources.

The production root is intentionally not made apply-ready in this PR. Before any production apply, complete focused remediation for:

- the final paid-subscription Azure region;
- production AKS node sizing and subscription quota;
- migration from the retired Azure Cache for Redis deployment model to Azure Managed Redis.

## Dev-Free Target

`envs/dev-free` is a separate simplified development root. This PR creates only the empty Terraform root and backend example. Later PRs can add Azure Container Apps Consumption, Storage Static Website, SQL free-offer evaluation, and temporary RabbitMQ/Redis container wiring.

The dev-free root must not duplicate the production AKS configuration, use conditional AKS flags, or share Terraform state with production.

## Relation To Existing Deployment Work

- `deploy/aks` contains placeholder Kubernetes manifests for the production AKS direction.
- [Azure SQL manual deployment runbook](../../docs/azure-sql-manual-deploy-runbook.md) remains the database schema process. EF migrations are not used.
- [AKS Key Vault and secrets strategy](../../docs/aks-key-vault-secrets-strategy.md) describes future workload-specific Key Vault wiring. Production Terraform enables cluster-level Workload Identity and Secrets Store CSI Driver support only; it does not add workload-specific identity resources, Key Vault permissions, SecretProviderClass resources, or real secrets.
- [RabbitMQ AKS production strategy](../../docs/rabbitmq-aks-production-strategy.md) remains a production follow-up. Terraform does not install RabbitMQ or the RabbitMQ Cluster Operator.
- [Angular Azure Static Hosting](../../docs/frontend-azure-static-hosting.md) documents the frontend model.

## Cost And Secret Warnings

AKS nodes, Azure SQL Database, Redis, Container Registry, Log Analytics, Key Vault operations, and Storage may incur cost in production. Review SKUs before applying.

Terraform state and plan files can contain sensitive values, including values passed through sensitive variables such as the Azure SQL administrator password. Treat the state storage account and blob container as sensitive assets. Remote state access must be tightly restricted.

Do not commit:

- Azure subscription IDs, tenant IDs, object IDs, client IDs, or production URLs;
- SQL admin passwords, connection strings, Redis keys, RabbitMQ credentials, or client secrets;
- `.tfstate`, `.tfplan`, `.terraform/`, crash logs, local `.tfvars` files, or local `backend.hcl` files.

The root `.gitignore` excludes Terraform state, plans, crash logs, local tfvars, and local backend config while keeping `*.tfvars.example` files trackable.

Terraform provider lock files should be reviewed when provider versions change. Commit `.terraform.lock.hcl` only after the selected provider versions are accepted.