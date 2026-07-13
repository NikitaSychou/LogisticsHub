# LogisticsHub Dev Terraform Environment

This environment defines the initial Azure foundation for future LogisticsHub AKS deployment work.

## Resources

- Resource group for the dev foundation.
- Azure Container Registry for future backend Docker images.
- AKS cluster with one cost-conscious system node pool.
- Log Analytics Workspace for AKS monitoring.
- Azure SQL logical server and three databases: `CompanyDb`, `InventoryDb`, and `ShipmentDb`.
- Azure Cache for Redis using a dev-appropriate Basic SKU by default.
- Azure Key Vault for future secret storage.
- Storage Account with static website hosting enabled for future Angular hosting.

This environment does not deploy Gateway, backend services, RabbitMQ, Key Vault CSI Driver, Workload Identity, Front Door, Application Gateway, Kubernetes resources, database schema, or Angular assets.

## Remote State

Run the bootstrap layer first. Then copy `backend.tf.example` to `backend.tf` and fill in the remote state resource group, storage account, and container values from bootstrap outputs.

Do not commit `backend.tf`; it is ignored because it contains environment-specific remote state settings. Keep `backend.tf.example` as the reviewed template.

The backend example uses Microsoft Entra ID / Azure AD-backed storage authentication with `use_azuread_auth = true`. Do not use access keys or SAS tokens for new remote state setup unless a reviewed exception is required. The Terraform operator or automation principal needs least-privilege blob data access to the state container, such as Storage Blob Data Contributor scoped to the container where feasible.

The AzureRM provider also uses Azure AD for Storage data-plane operations with `storage_use_azuread = true`. If Terraform cannot create or read blob containers because of an Azure AD authorization error, review the Terraform principal's Storage Blob Data permissions.

Terraform state and plan files can contain sensitive values, including sensitive inputs such as `sql_admin_password`. Treat remote state as sensitive infrastructure.

## Required Local Values

Copy `dev.tfvars.example` to an uncommitted `.tfvars` file and replace all `<unique>` placeholders. Supply `sql_admin_password` through `TF_VAR_sql_admin_password` or an uncommitted local tfvars file.

Do not commit passwords, tenant-specific identifiers, subscription IDs, connection strings, Redis keys, or production URLs.

## Review Workflow

Before planning changes, run:

```powershell
terraform fmt -check -recursive ..\..
terraform init
terraform validate
terraform plan -var-file=<local-file>.tfvars
```

Run `terraform apply` only after reviewing the plan and confirming the selected region and cost assumptions.

## Region And Cost Review

The example uses `westeurope` as the default candidate. Before applying, compare `westeurope` and `northeurope` in the Azure Pricing Calculator for:

- AKS node VM size and node count;
- Azure SQL database SKU;
- Redis SKU;
- Storage Account settings;
- Log Analytics ingestion expectations.

Use `northeurope` if it is cheaper for the selected resource set.

AKS nodes, Azure SQL, Redis, ACR, Storage, Log Analytics, and Key Vault may incur cost.

## Future Hardening

- Replace password-based SQL admin with reviewed Microsoft Entra administration/passwordless access if appropriate.
- Add private networking and firewall rules for Azure SQL, Redis, and Key Vault.
- Attach AKS to ACR with least privilege.
- Add AKS Workload Identity and Key Vault CSI wiring.
- Add RabbitMQ Cluster Operator deployment.
- Add Front Door/CDN and Gateway ingress/TLS.
- Add CI/CD plan/apply workflows with approval gates.
