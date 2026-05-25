# Local Run Guide

This guide covers the local application workflow for reviewers and contributors.

## Prerequisites

- .NET SDK 10
- SQL Server available locally or through Docker Compose
- RabbitMQ available locally or through Docker Compose
- Redis available locally or through Docker Compose for future local infrastructure work

Docker Compose can start SQL Server, RabbitMQ, Redis, Gateway, CompanyService, InventoryService, and ShipmentService for local development:

```powershell
docker compose up --build
```

The compose file does not create database schema. `InventoryDb` and `ShipmentDb` must be prepared manually before full application flow testing. The current local SQL Express schema is documented in [Database schema](database-schema.md).

The local appsettings used by `dotnet run` still point to local SQL Server databases:

- `InventoryDb`
- `ShipmentDb`

The expected local SQL Server instance for the checked-in local appsettings is `localhost\SQLEXPRESS` with Windows Authentication.

For containers, `docker-compose.yml` overrides connection strings and RabbitMQ settings so services use Docker service names such as `sqlserver` and `rabbitmq`.

Redis is exposed by Docker Compose for local infrastructure integration work, but the current application code does not use Redis yet.

When running the full application through Docker Compose, prepare the SQL Server container databases from the repository root after SQL Server starts:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\bootstrap-docker-sql.ps1
```

This creates `InventoryDb` and `ShipmentDb` in the `logisticshub-sqlserver` container if needed and applies the checked-in schema snapshots.

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
2. RabbitMQ

You can run everything through Docker Compose, or use locally installed dependencies with `dotnet run`.

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

Gateway Swagger documents Gateway endpoints only. Use the direct service Swagger pages for InventoryService and ShipmentService APIs.
CompanyService currently exposes only shell endpoints such as `/health` and Development Swagger; Company/Address CRUD is not implemented yet.

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
| SQL Server | `localhost,1433` |

`depends_on` controls container start order only. SQL Server and RabbitMQ may still need time to become ready, so check container logs if a service fails during startup.

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
