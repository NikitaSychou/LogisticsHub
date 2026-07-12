# AKS Key Vault And Secrets Strategy

This document defines the intended production secrets and configuration approach for the future LogisticsHub AKS deployment. It does not implement Azure Key Vault, Workload Identity, Secrets Store CSI Driver, CI/CD, or real cloud secrets.

## Target Approach

- Use Azure Key Vault as the external source for production secrets.
- Use AKS Workload Identity later so pods can access Azure resources without committed credentials.
- Use Secrets Store CSI Driver or an equivalent Key Vault integration later to make required secrets available to workloads.
- Keep Kubernetes `Secret` manifests with base64 production values out of source control.
- Keep local `.env` files local-only. `.env.example` must contain placeholders only.

The current `deploy/aks` skeleton references a placeholder Kubernetes secret named `logisticshub-backend-secrets`. That name is a review-time placeholder for the future secret source, not a committed production secret.

## Configuration Classification

Use ConfigMaps for non-secret, environment-specific configuration:

| Category | Examples |
|---|---|
| Runtime environment | `ASPNETCORE_ENVIRONMENT`, `DOTNET_ENVIRONMENT` |
| Internal service discovery | Gateway reverse proxy destination URLs, `CompanyService__BaseUrl` |
| Non-secret tuning | retry counts, timeout values, circuit breaker settings |
| Non-secret messaging config | RabbitMQ exchange name; host/port if internal service names are acceptable to expose |
| Non-secret Entra config | authority/instance, tenant ID, API app client ID, audience, required scope, Swagger OAuth client ID/scope, if the deployment treats app registration identifiers as public configuration |

Use Key Vault-backed secrets for sensitive values:

| Category | Examples |
|---|---|
| Azure SQL | service database connection strings, SQL usernames, SQL passwords |
| Redis | access keys or full connection strings |
| RabbitMQ | username and password |
| OAuth confidential clients | any client secret or certificate if one is introduced later |
| Private endpoints or credentials | any value that grants access or reveals private infrastructure beyond normal service discovery |

Angular `runtime-config.json` must not contain secrets. SPA client IDs, authorities, redirect URIs, and scopes are public runtime configuration; client secrets do not belong in the Angular app.

## Production Safety Rules

- Do not commit production connection strings, passwords, tokens, certificates, or private URLs.
- Do not commit base64 Kubernetes Secret values for production.
- Do not put production secrets in `appsettings.json`, Docker Compose files, checked-in Kubernetes YAML, or docs.
- Do not print secret values in startup logs, health checks, tests, or troubleshooting output.
- Rotate database, Redis, and RabbitMQ credentials through the chosen secret-management process.
- Prefer separate identities and permissions per environment.
- Grant each workload only the Key Vault permissions it needs.
- Treat local Docker Compose `.env` values as local development values only.

## Current Secret Categories

| Area | Current key shape | Production source |
|---|---|---|
| CompanyDb | `ConnectionStrings__CompanyDb` | Key Vault-backed secret |
| InventoryDb | `ConnectionStrings__InventoryDb` | Key Vault-backed secret |
| ShipmentDb | `ConnectionStrings__ShipmentDb` | Key Vault-backed secret |
| Redis | `ConnectionStrings__Redis` | Key Vault-backed secret |
| RabbitMQ credentials | `RabbitMq__UserName`, `RabbitMq__Password` | Key Vault-backed secret |
| RabbitMQ routing | `RabbitMq__HostName`, `RabbitMq__Port`, `RabbitMq__ExchangeName` | ConfigMap unless the value is considered private |
| Entra API settings | `AzureAd__Instance`, `AzureAd__TenantId`, `AzureAd__ClientId`, `AzureAd__Audience`, `AzureAd__RequiredScope` | ConfigMap unless deployment policy classifies identifiers as sensitive |
| Swagger OAuth | `SwaggerOAuth__ClientId`, `SwaggerOAuth__Scope` | ConfigMap unless deployment policy classifies identifiers as sensitive |

If a future service uses a confidential client credential, store that credential in Key Vault and do not add it to Angular runtime config.

## Future Implementation Checklist

1. Create the Azure Key Vault for the target environment.
2. Store Azure SQL, Redis, RabbitMQ, and any confidential-client secrets in Key Vault.
3. Enable and configure AKS Workload Identity.
4. Assign Kubernetes service accounts to workload identities.
5. Grant each identity minimal Key Vault permissions for only the secrets it needs.
6. Add Secrets Store CSI Driver or the selected Key Vault integration.
7. Wire Key Vault-backed values into pod environment variables or mounted files.
8. Move non-secret values into ConfigMaps where appropriate.
9. Verify pods start without committed secrets or local `.env` files.
10. Verify Azure SQL, Redis, and RabbitMQ connectivity.
11. Verify secret rotation and rollback for at least one representative secret.

## What This PR Does Not Do

- It does not add Key Vault resources, CSI Driver manifests, Workload Identity manifests, Helm, Terraform, Bicep, or CI/CD workflows.
- It does not add real Azure resource names, tenant IDs, client IDs, object IDs, URLs, passwords, or connection strings.
- It does not change application code, Angular runtime config, backend auth, service-to-service auth, RabbitMQ behavior, Docker Compose, or EF/database migration rules.
