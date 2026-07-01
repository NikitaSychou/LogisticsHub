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

Current tests are Application-level tests under `tests/`. They do not require RabbitMQ or SQL Server.

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
