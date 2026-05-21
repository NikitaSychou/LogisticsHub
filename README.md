# LogisticsHub

LogisticsHub is a microservices backend project for a shipment and inventory workflow. It demonstrates service boundaries, asynchronous messaging, idempotency, and SQL Server persistence.

## Architecture

- **Gateway** - YARP reverse proxy.
- **InventoryService** - inventory items, stock balances, and stock reservations.
- **ShipmentService** - shipment creation and reservation status tracking.
- **SQL Server database per service** - `InventoryDb` and `ShipmentDb`.
- **RabbitMQ integration events** - asynchronous service communication.
- **Outbox/inbox idempotency** - reliable publishing and duplicate message handling.
- **MediatR CQRS-style handlers** - application use cases are commands and queries.
- **Mapperly API mapping** - source-generated DTO mapping in API projects.

API request flow:

```text
Controller -> Validator -> Mapperly mapper -> IMediator -> Application handler -> HTTP response
```

## Main Flow

```text
POST /shipments
  -> ShipmentService outbox
  -> RabbitMQ
  -> InventoryService
  -> reservation result event
  -> ShipmentService
```

Duplicate `EventId` deliveries are ignored through inbox tables. Shipment result handlers also guard state so stale reservation result events do not overwrite final shipment states.

## Local URLs

| Service | URL |
|---|---|
| Gateway | `http://localhost:5100` |
| InventoryService | `http://localhost:5101` |
| ShipmentService | `http://localhost:5102` |

Swagger UI is available at `/swagger` in Development.

RabbitMQ and SQL Server must be running locally. Docker Compose is not present yet.

## Build

```powershell
dotnet restore .\LogisticsHub.sln
dotnet build .\LogisticsHub.sln
```

## Database Rule

EF Core migrations are intentionally not used in this project.

Do not run:

```powershell
dotnet ef migrations add
dotnet ef database update
```

Database changes should be made with manual SQL scripts.

## Development Workflow

1. Create a feature branch from `master`.
2. Open a pull request to `master`.
3. Wait for GitHub Actions to pass.
4. Merge after checks pass.
