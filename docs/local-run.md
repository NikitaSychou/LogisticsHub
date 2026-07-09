# Local Run Guide

This guide covers the local application workflow for reviewers and contributors.

## Prerequisites

- .NET SDK 10
- SQL Server Express available on the Windows host as `localhost\SQLEXPRESS`
- SQL Server TCP/IP enabled on fixed port `14330`
- `CompanyDb`, `InventoryDb`, and `ShipmentDb` created and manually prepared from the checked-in SQL scripts
- SQL logins and database users configured for the three application services
- RabbitMQ and Redis available through Docker Compose

Docker Compose starts RabbitMQ, Redis, Gateway, CompanyService, InventoryService, and ShipmentService for local development:

```powershell
docker compose up --build
```

Docker Compose does not start SQL Server and does not create database schema automatically. `InventoryDb`, `ShipmentDb`, and the manual `CompanyDb` baseline are documented in [Database schema](database-schema.md).

Create a local root `.env` file before running Compose. The file is ignored by Git, must not be committed, and supplies the service database password variables:

- `COMPANYSERVICE_DB_PASSWORD`
- `INVENTORYSERVICE_DB_PASSWORD`
- `SHIPMENTSERVICE_DB_PASSWORD`
- `AZUREAD_INSTANCE`
- `AZUREAD_TENANT_ID`
- `AZUREAD_CLIENT_ID`
- `AZUREAD_AUDIENCE`
- `SWAGGEROAUTH_CLIENT_ID`
- `SWAGGEROAUTH_SCOPE`

The checked-in `AzureAd` appsettings values are intentionally empty. For `dotnet run`, provide the backend auth settings through .NET User Secrets or environment variables:

- `AzureAd:Instance` = `https://login.microsoftonline.com`
- `AzureAd:TenantId` = `<tenant-id>`
- `AzureAd:ClientId` = `<api-app-client-id>`
- `AzureAd:Audience` = `<api-app-client-id>`
- `AzureAd:RequiredScope` = `api://<api-app-client-id>/access_as_user`

For the current Microsoft Entra ID v2 token setup, `AzureAd:Audience` and `AZUREAD_AUDIENCE` must be the API application client ID without the `api://` prefix. Do not use the full scope as the audience. Request tokens with this scope:

```text
api://<api-app-client-id>/access_as_user
```

The API app registration manifest must set `api.requestedAccessTokenVersion` to `2` so access tokens use the v2 issuer expected by the backend.

Docker Compose reads the same auth settings from `AZUREAD_INSTANCE`, `AZUREAD_TENANT_ID`, `AZUREAD_CLIENT_ID`, and `AZUREAD_AUDIENCE` in the local `.env` file. The backend scope policy uses `AzureAd:RequiredScope`; for current local Compose compatibility, it falls back to `SwaggerOAuth:Scope` when `AzureAd:RequiredScope` is not set.

Swagger UI OAuth uses authorization code with PKCE. Set `SwaggerOAuth:ClientId` to the Entra client app registration ID used by Swagger/local frontend, not the API app registration ID. Set `SwaggerOAuth:Scope` to:

```text
api://<api-app-client-id>/access_as_user
```

For Docker Compose, set `SWAGGEROAUTH_CLIENT_ID` and `SWAGGEROAUTH_SCOPE` in `.env`. Add these redirect URIs to the Entra client app registration used by Swagger:

- `http://localhost:5100/swagger/oauth2-redirect.html`
- `http://localhost:5101/swagger/oauth2-redirect.html`
- `http://localhost:5102/swagger/oauth2-redirect.html`
- `http://localhost:5103/swagger/oauth2-redirect.html`

Quick checks:

```powershell
curl -i http://localhost:5100/health
curl -i http://localhost:5100/company/companies
curl -i -H "Authorization: Bearer <access-token>" http://localhost:5100/company/companies
```

Health should return `200` without a token. A protected API call without a token should return `401`. With a Bearer token, the protected call should return `200` or another non-`401` application response.

The local appsettings used by `dotnet run` still point to local SQL Server databases:

- `InventoryDb`
- `ShipmentDb`
- `CompanyDb`

The expected local SQL Server instance for the checked-in local appsettings is `localhost\SQLEXPRESS` with Windows Authentication.
CompanyService connects to `CompanyDb` for health checks and minimal Company/Address CRUD.
ShipmentService validates required sender/receiver company/address references through CompanyService during shipment creation.

For containers, `docker-compose.yml` overrides connection strings so application services connect to host SQL Server through `host.docker.internal,14330`. RabbitMQ and Redis use Docker service names.
It also points ShipmentService at `http://companyservice:8080` for required company/address reference validation.

Redis is exposed by Docker Compose and CompanyService uses it as a cache for company address detail lookups. If Redis is unavailable, CompanyService falls back to CompanyDb.

## Services

| Service | Project | Local URL |
|---|---|---|
| Gateway | `src/Gateway/LogisticsHub.Gateway` | `http://localhost:5100` |
| CompanyService | `src/CompanyService/LogisticsHub.CompanyService` | `http://localhost:5103` |
| InventoryService | `src/InventoryService/LogisticsHub.InventoryService` | `http://localhost:5101` |
| ShipmentService | `src/ShipmentService/LogisticsHub.ShipmentService` | `http://localhost:5102` |

Configured ports are in each service's `Properties/launchSettings.json`.

## Run Order

Start local dependencies first:

1. SQL Server
2. RabbitMQ and Redis

You can run the application services, RabbitMQ, and Redis through Docker Compose, or use locally installed dependencies with `dotnet run`.

To run the backend services directly on the host:

```powershell
dotnet run --project .\src\InventoryService\LogisticsHub.InventoryService\LogisticsHub.InventoryService.csproj
dotnet run --project .\src\ShipmentService\LogisticsHub.ShipmentService\LogisticsHub.ShipmentService.csproj
dotnet run --project .\src\CompanyService\LogisticsHub.CompanyService\LogisticsHub.CompanyService.csproj
dotnet run --project .\src\Gateway\LogisticsHub.Gateway\LogisticsHub.Gateway.csproj
```

Swagger UI is available in Development:

| Service | Swagger UI |
|---|---|
| Gateway | `http://localhost:5100/swagger` |
| CompanyService | `http://localhost:5103/swagger` |
| InventoryService | `http://localhost:5101/swagger` |
| ShipmentService | `http://localhost:5102/swagger` |

Gateway Swagger documents Gateway endpoints only. Use the direct service Swagger pages for service APIs.
CompanyService exposes `/health`, `/health/live`, `/health/ready`, Development Swagger, and minimal Company/Address CRUD. Through Gateway, CompanyService routes are available under `/company`.

For a Gateway-first end-to-end check of inventory creation, shipment creation, RabbitMQ stock reservation, and final shipment status, see [Manual smoke test](manual-smoke-test.md).

## CacheWorker RunOnce

The CacheWorker can be run directly from the host to warm CompanyService cache entries from `CompanyDb` into Redis. It uses `CompanyDb` as the source of truth and writes only rebuildable cached copies. Deleted rows are not refreshed; old keys expire by TTL unless normal API invalidation removes them earlier.

Required local dependencies:

- SQL Server Express with `CompanyDb` prepared from the checked-in SQL scripts
- Redis on `localhost:6379`

Run all warm-up modules once:

```powershell
dotnet run --project .\src\LogisticsHub.Workers.CacheWorker\LogisticsHub.Workers.CacheWorker.csproj -- --CacheWorker:RunOnce=true
```

Run only company detail cache warm-up:

```powershell
dotnet run --project .\src\LogisticsHub.Workers.CacheWorker\LogisticsHub.Workers.CacheWorker.csproj -- --CacheWorker:RunOnce=true --CacheWorker:EnabledModules:0=companies
```

Run only company address detail cache warm-up:

```powershell
dotnet run --project .\src\LogisticsHub.Workers.CacheWorker\LogisticsHub.Workers.CacheWorker.csproj -- --CacheWorker:RunOnce=true --CacheWorker:EnabledModules:0=company-addresses
```

CacheWorker has no HTTP endpoint.

Build the CacheWorker image:

```powershell
docker build -f .\src\LogisticsHub.Workers.CacheWorker\Dockerfile -t logisticshub-cacheworker .
```

Run all warm-up modules once through Docker Compose:

```powershell
docker compose --profile cacheworker run --rm cacheworker
```

Run only company detail cache warm-up through Docker Compose:

```powershell
docker compose --profile cacheworker run --rm -e CacheWorker__EnabledModules__0=companies cacheworker
```

Run only company address detail cache warm-up through Docker Compose:

```powershell
docker compose --profile cacheworker run --rm -e CacheWorker__EnabledModules__0=company-addresses cacheworker
```

## Docker Compose Notes

Compose exposes the same local service ports:

| Service | URL |
|---|---|
| Gateway | `http://localhost:5100` |
| CompanyService | `http://localhost:5103` |
| InventoryService | `http://localhost:5101` |
| ShipmentService | `http://localhost:5102` |
| RabbitMQ Management | `http://localhost:15672` |
| Redis | `localhost:6379` |
| SQL Server from containers | `host.docker.internal,14330` |
| SQL Server from host tools | `localhost\SQLEXPRESS` |

`depends_on` controls container start order only. RabbitMQ may still need time to become ready, so check container logs if a service fails during startup. SQL Server is not managed by Docker Compose.

To check the Redis container directly:

```powershell
docker compose exec redis redis-cli ping
```

Expected response:

```text
PONG
```

## Tests

Run all tests with:

```powershell
dotnet test .\LogisticsHub.sln
```

Most tests under `tests/` are in-memory application-level tests and do not require manually started RabbitMQ or SQL Server. The RabbitMQ integration tests use Testcontainers, so running the full solution test suite requires Docker to be available.

## Database Rule

EF Core migrations are intentionally not used. Do not run:

```powershell
dotnet ef migrations add
dotnet ef database update
```

Database schema changes should be handled with manual SQL outside EF migrations.

After manual schema changes, refresh the schema snapshots from the repository root:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\export-local-db-schema.ps1
```
