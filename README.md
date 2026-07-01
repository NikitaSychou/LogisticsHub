# LogisticsHub

LogisticsHub is a microservices backend project for a shipment and inventory workflow. It demonstrates service boundaries, asynchronous messaging, idempotency, and SQL Server persistence.

## Architecture

- **Gateway** - YARP reverse proxy.
- **CompanyService** - company master data and company addresses backed by CompanyDb.
- **InventoryService** - inventory items, stock balances, and stock reservations.
- **ShipmentService** - shipment creation with required sender/receiver company/address references and reservation status tracking.
- **SQL Server database per service** - `InventoryDb`, `ShipmentDb`, and the manual `CompanyDb` baseline.
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
| CompanyService | `http://localhost:5103` |
| InventoryService | `http://localhost:5101` |
| ShipmentService | `http://localhost:5102` |

Swagger UI is available in Development:

- Gateway: `http://localhost:5100/swagger`
- CompanyService: `http://localhost:5103/swagger`
- InventoryService: `http://localhost:5101/swagger`
- ShipmentService: `http://localhost:5102/swagger`

Gateway Swagger documents Gateway endpoints only; use the direct service Swagger pages for service APIs.

Docker Compose starts RabbitMQ, Redis, and the four ASP.NET Core services for local review. SQL Server runs on the Windows host as `SQLEXPRESS`; containers connect to it through `host.docker.internal,14330`. You can still run the .NET services directly with `dotnet run`.
CompanyService uses Redis as a cache for `GET /companies/{companyId}/addresses/{addressId}`. CompanyDb remains the source of truth.

Each service exposes lightweight liveness at `/health/live` and readiness at `/health/ready`. The existing `/health` endpoint remains a readiness-compatible check. CompanyService readiness checks CompanyDb connectivity. InventoryService and ShipmentService readiness checks verify RabbitMQ connectivity. They do not validate every exchange, queue, or binding.

CompanyService exposes the minimal local company/address API through the Gateway under `/company`, including `POST /company/companies`, `GET /company/companies/{id}`, `GET /company/companies`, `PUT /company/companies/{id}`, `POST /company/companies/{companyId}/addresses`, `GET /company/companies/{companyId}/addresses`, and `GET /company/companies/{companyId}/addresses/{addressId}`. Shipment creation requires sender/receiver company and address IDs; ShipmentService validates those pairs through CompanyService before saving.

Outbox publishers use row claiming for multiple replicas and bounded retry scheduling with a poison-message state for messages that keep failing.

For local setup and operations notes, see:

- [Local run guide](docs/local-run.md)
- [Database schema](docs/database-schema.md)
- [Manual smoke test](docs/manual-smoke-test.md)
- [Troubleshooting](docs/troubleshooting.md)

## Build

```powershell
dotnet restore .\LogisticsHub.sln
dotnet build .\LogisticsHub.sln
dotnet test .\LogisticsHub.sln
```

## Docker Compose

```powershell
docker compose up --build
```

Docker Compose does not start SQL Server and does not create database schema automatically. Prepare `InventoryDb`, `ShipmentDb`, and `CompanyDb` manually on the host `SQLEXPRESS` instance from the checked-in SQL scripts. The root `.env` file supplies the service database password variables used by Compose:

- `COMPANYSERVICE_DB_PASSWORD`
- `INVENTORYSERVICE_DB_PASSWORD`
- `SHIPMENTSERVICE_DB_PASSWORD`

The current business smoke-test path uses all three databases because ShipmentService validates required sender/receiver references through CompanyService.

The current local SQL Express schema can be exported with `export-local-db-schema.ps1`; see [Database schema](docs/database-schema.md).

For the smallest full local workflow through the Gateway, see [Manual smoke test](docs/manual-smoke-test.md).

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
