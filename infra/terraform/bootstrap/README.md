# Terraform Remote State Bootstrap

This layer creates the Azure Storage backend used by the dev Terraform environment.

## Resources

- Resource group for Terraform state.
- Storage account for Terraform state.
- Private blob container for Terraform state.

The state storage account disables shared key access and is intended for Microsoft Entra ID / Azure AD and Azure RBAC-backed Terraform backend access. Blob public access is disabled at the account level, and the state container is private.

## Usage

1. Copy `terraform.tfvars.example` to a local `.tfvars` file.
2. Replace placeholder names with globally unique, reviewed values.
3. Run `terraform init`.
4. Run `terraform plan -var-file=<local-file>.tfvars`.
5. Apply manually only after review.

Do not commit local `.tfvars`, `.tfstate`, `.tfplan`, crash logs, or `.terraform/` directories.

Terraform state and plan files can contain sensitive values. Protect state access with least privilege; the operator or automation principal needs blob data access to the state container, for example Storage Blob Data Contributor scoped to the container where feasible.

After bootstrap, copy the outputs into `../envs/dev/backend.tf` from `backend.tf.example`.

Private endpoints, network firewall rules, customer-managed keys, and production-grade state storage monitoring are future hardening items and are intentionally not added in this foundation PR.
