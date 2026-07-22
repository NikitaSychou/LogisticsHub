# LogisticsHub Dev Terraform Environment

This environment defines the initial Azure foundation for future LogisticsHub AKS deployment work.

## Resources

- Resource group for the dev foundation.
- Explicit virtual network, dedicated AKS subnet, and AKS subnet network security group.
- Azure Container Registry for future backend Docker images.
- AKS cluster with one cost-conscious system node pool and Secrets Store CSI Driver support for Azure Key Vault.
- Log Analytics Workspace for AKS monitoring.
- Azure SQL logical server and three databases: `CompanyDb`, `InventoryDb`, and `ShipmentDb`.
- Azure Cache for Redis using a dev-appropriate Basic SKU by default.
- Azure Key Vault for future secret storage.
- Storage Account with static website hosting enabled for future Angular hosting.

This environment does not deploy Gateway, backend services, RabbitMQ, Front Door, Application Gateway, Kubernetes resources, database schema, Angular assets, private endpoints, Private DNS zones, NAT Gateway, Azure Firewall, ingress, custom route tables, SecretProviderClass resources, secret volume mounts, Kubernetes Secret synchronization, or workload-specific Azure identity resources.

## Remote State

`backend.tf` is tracked and contains only the partial `azurerm` backend declaration. Keep environment-specific backend settings in a local `backend.hcl` file, which is ignored by Git.

Run the bootstrap layer first. Then copy `backend.tf.example` to `backend.hcl` and fill in the remote state resource group, storage account, and container values from bootstrap outputs.

Do not commit `backend.hcl`; it contains environment-specific remote state settings. Keep `backend.tf.example` as the reviewed template for local backend configuration.

Initialize the backend with `terraform init -reconfigure -backend-config="backend.hcl"`. Backend initialization does not require `terraform apply`.

The backend uses Microsoft Entra ID / Azure AD-backed storage authentication with `use_azuread_auth = true`. Do not use access keys or SAS tokens for new remote state setup unless a reviewed exception is required. The Terraform operator or automation principal needs least-privilege blob data access to the state container, such as Storage Blob Data Contributor scoped to the container where feasible.

The AzureRM provider also uses Azure AD for Storage data-plane operations with `storage_use_azuread = true`. If Terraform cannot create or read blob containers because of an Azure AD authorization error, review the Terraform principal's Storage Blob Data permissions.

Terraform state and plan files can contain sensitive values, including sensitive inputs such as `sql_admin_password`. Treat remote state as sensitive infrastructure.

## Networking

The dev AKS cluster uses a Terraform-managed VNet and dedicated AKS subnet before the first apply. The default CIDR plan is:

- VNet: `10.20.0.0/16`
- AKS subnet: `10.20.0.0/22`
- Reserved future private-endpoint subnet range: `10.20.4.0/24`
- AKS service CIDR: `10.30.0.0/16`
- AKS DNS service IP: `10.30.0.10`
- AKS pod CIDR: `10.40.0.0/16`

The private-endpoint range is reserved through variables and documentation only; this PR does not create a private-endpoint subnet, private endpoints, or Private DNS zones.

AKS uses Azure CNI Overlay with Azure network policy and load balancer outbound egress. The AKS default node pool is placed in the dedicated AKS subnet, where Azure default outbound access is explicitly disabled so egress uses the AKS Standard Load Balancer outbound path.

The AKS subnet has an explicit NSG association. No custom inbound NSG rules are added because the current AKS model does not require broad inbound access, unrestricted SSH, or custom Kubernetes API access rules. Azure platform defaults and AKS-managed load balancer behavior are sufficient for this foundation.

No custom route table is created. Azure CNI Overlay with `outbound_type = "loadBalancer"` does not require a custom UDR for this dev foundation. Add a route table later only if the architecture adopts Azure Firewall, NAT Gateway with explicit routing requirements, forced tunnelling, an NVA, or another custom egress design.

Terraform validates CIDR syntax and the DNS service IP convention. Terraform 1.6-compatible configuration does not include a simple built-in CIDR-overlap predicate, so review any changed CIDRs manually and keep the VNet, AKS service CIDR, and AKS pod CIDR non-overlapping. No repository-documented local development CIDR currently conflicts with the default plan.

AKS has its OIDC issuer and Microsoft Entra Workload Identity enabled at the cluster level. Secrets Store CSI Driver support with the Azure Key Vault provider and mounted secret rotation is also enabled at the cluster level. These settings let future Kubernetes workloads authenticate to Azure and mount Key Vault content without storing long-lived client secrets, but they do not grant Azure resource access by themselves. The AKS kubelet identity receives only AcrPull access to this environment's ACR so nodes can pull images without ACR admin credentials or image pull secrets. Workload-specific managed identities, federated identity credentials, Kubernetes service accounts, Key Vault roles, SecretProviderClass resources, and workload secret mounts are intentionally deferred. No real secret values are stored in Terraform or Git.

## Required Local Values

Copy `dev.tfvars.example` to an uncommitted `.tfvars` file and replace all `<unique>` placeholders. Supply `sql_admin_password` through `TF_VAR_sql_admin_password` or an uncommitted local tfvars file.

Do not commit passwords, tenant-specific identifiers, subscription IDs, connection strings, Redis keys, or production URLs.

## Review Workflow

Before planning changes, run:

```powershell
terraform fmt -check -recursive ..\..
terraform init -reconfigure -backend-config="backend.hcl"
terraform validate
terraform plan -var-file=<local-file>.tfvars
```

For focused Terraform PRs, stop after review and `terraform plan`; do not run `terraform apply`.

Run `terraform apply` only after reviewing the plan and confirming the selected region and cost assumptions.

## Region And Cost Review

The dev example currently uses `northeurope` for the selected resource set. Before applying, compare `northeurope` and `westeurope` in the Azure Pricing Calculator for:

- AKS node VM size and node count;
- Azure SQL database SKU;
- Redis SKU;
- Storage Account settings;
- Log Analytics ingestion expectations;
- VNet, subnet, and NSG settings.

Use `northeurope` for the current selected resource set unless a future review changes the region decision.

AKS nodes, Azure SQL, Redis, ACR, Storage, Log Analytics, and Key Vault may incur cost.

## Future Hardening

- Replace password-based SQL admin with reviewed Microsoft Entra administration/passwordless access if appropriate.
- Add private networking and firewall rules for Azure SQL, Redis, and Key Vault.
- Attach AKS to ACR with least privilege.
- Add workload-specific managed identities, federated identity credentials, Kubernetes service accounts, Key Vault role assignments, SecretProviderClass resources, and secret volume mounts.
- Add RabbitMQ Cluster Operator deployment.
- Add Front Door/CDN and Gateway ingress/TLS.
- Add CI/CD plan/apply workflows with approval gates.