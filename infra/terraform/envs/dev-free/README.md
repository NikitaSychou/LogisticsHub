# LogisticsHub Dev-Free Terraform Environment

This root is reserved for the simplified low-cost Azure development target. It is intentionally empty in this PR: no Azure resources are declared yet.

## Planned Direction

- Azure Container Apps Consumption will replace AKS for the simplified dev path.
- RabbitMQ and Redis are planned as temporary development containers rather than managed Azure services.
- Azure SQL free-offer databases will be evaluated separately before any resources are added.
- Storage Static Website remains the planned frontend hosting target.

Do not add Kubernetes, AKS, route tables, private endpoints, RabbitMQ, Redis, SQL, Container Apps, or application deployment resources in this root until a focused follow-up PR implements them.

## Remote State

`backend.tf` is tracked and contains only the partial `azurerm` backend declaration. Keep environment-specific backend settings in a local ignored `backend.hcl` file.

Copy `backend.tf.example` to `backend.hcl` after bootstrap and keep the state key separate from other environments:

- `logisticshub/dev-free.tfstate`

Initialize with Azure AD authentication:

```powershell
terraform init -reconfigure -backend-config="backend.hcl"
```

Backend initialization does not require `terraform apply`.

## Validation

This empty root should remain valid with:

```powershell
terraform init -backend=false -input=false
terraform validate
```