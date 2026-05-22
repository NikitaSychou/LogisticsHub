# LogisticsHub

LogisticsHub is a microservices backend project for a shipment and inventory workflow. It demonstrates service boundaries, asynchronous messaging, idempotency, and SQL Server persistence.

## Architecture

- **Gateway** - YARP reverse proxy.
- **InventoryService** - inventory items, stock balances, and stock reservations.
- **ShipmentService** - shipment creation and reservation status tracking.
- **SQL Server database per service** - `InventoryDb` and `ShipmentDb`.
- **RabbitMQ integration events** - asynchronous service communication.
- **Outbox/inbox idempotency** - reliable publishing, duplicate message handling, and manual retry visibility.
- **RabbitMQ reliability basics** - bounded consumer retry, DLQs, and RabbitMQ health checks for InventoryService and ShipmentService.
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

Duplicate `EventId` deliveries are ignored through inbox tables. Shipment result handlers also guard state so stale reservation result events do not overwrite final shipment states. Failed consumer messages are retried briefly, then sent to RabbitMQ dead-letter queues.

## Local URLs

| Service | URL |
|---|---|
| Gateway | `http://localhost:5100` |
| InventoryService | `http://localhost:5101` |
| ShipmentService | `http://localhost:5102` |

Swagger UI is available in Development:

- Gateway: `http://localhost:5100/swagger`
- InventoryService: `http://localhost:5101/swagger`
- ShipmentService: `http://localhost:5102/swagger`

Gateway Swagger documents Gateway endpoints only; use the direct service Swagger pages for InventoryService and ShipmentService APIs.

RabbitMQ and SQL Server must be running locally. Docker Compose can start these local dependencies; the .NET services still run with `dotnet run`.

InventoryService and ShipmentService `/health` endpoints check RabbitMQ connectivity. They do not validate every exchange, queue, or binding.

Outbox publishers use row claiming for multiple replicas and bounded retry scheduling with a poison-message state for messages that keep failing.

For local setup and operations notes, see:

- [Local run guide](docs/local-run.md)
- [Troubleshooting](docs/troubleshooting.md)

## Build

```powershell
dotnet restore .\LogisticsHub.sln
dotnet build .\LogisticsHub.sln
dotnet test .\LogisticsHub.sln
```

## Tests

Test projects live under `tests/`. The current test suite uses Application-level tests for reservation reliability behavior and does not require RabbitMQ or SQL Server.

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
3. Wait for GitHub Actions to restore, build, and test the solution.
4. Merge after checks pass.
