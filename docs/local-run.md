# Local Run Guide

This guide covers the local application workflow for reviewers and contributors.

## Prerequisites

- .NET SDK 10
- SQL Server available locally
- RabbitMQ available locally

The service connection strings currently point to local SQL Server databases:

- `InventoryDb`
- `ShipmentDb`

RabbitMQ is configured through each service's `RabbitMq` section and currently uses `localhost:5672`.

## Services

| Service | Project | Local URL |
|---|---|---|
| Gateway | `src/Gateway/LogisticsHub.Gateway` | `http://localhost:5100` |
| InventoryService | `src/InventoryService/LogisticsHub.InventoryService` | `http://localhost:5101` |
| ShipmentService | `src/ShipmentService/LogisticsHub.ShipmentService` | `http://localhost:5102` |

Configured ports are in each service's `Properties/launchSettings.json`.

## Run Order

Start local dependencies first:

1. SQL Server
2. RabbitMQ

Then start the backend services:

```powershell
dotnet run --project .\src\InventoryService\LogisticsHub.InventoryService\LogisticsHub.InventoryService.csproj
dotnet run --project .\src\ShipmentService\LogisticsHub.ShipmentService\LogisticsHub.ShipmentService.csproj
dotnet run --project .\src\Gateway\LogisticsHub.Gateway\LogisticsHub.Gateway.csproj
```

Swagger UI is available at `/swagger` in Development for the services that expose APIs.

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
