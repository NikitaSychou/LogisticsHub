# Troubleshooting

This project uses host SQL Server Express, Docker Compose RabbitMQ and Redis, Gateway, CompanyService, InventoryService, and ShipmentService. Start with the smallest check that proves which part is failing.

## Startup Checks

1. Confirm `SQLEXPRESS` is running, TCP/IP is enabled, and port `14330` is listening.
2. Confirm RabbitMQ is running on the configured host and port.
3. Start InventoryService and ShipmentService before the Gateway.
4. Check each service console for configuration, database, or RabbitMQ connection errors.

If using Docker Compose, check container status with `docker compose ps`. RabbitMQ management is available at `http://localhost:15672` with the local development credentials from the container image defaults. SQL Server is not a Compose service; application containers connect to the host through `host.docker.internal,14330`.

`depends_on` starts containers in order but does not guarantee that RabbitMQ is fully ready. If an app container exits during startup, check logs with:

```powershell
docker compose logs inventoryservice
docker compose logs shipmentservice
docker compose logs companyservice
docker compose logs gateway
```

## Health Endpoints

| Service | Liveness endpoint | Readiness endpoint |
|---|---|---|
| Gateway | `http://localhost:5100/health/live` | `http://localhost:5100/health/ready` |
| CompanyService | `http://localhost:5103/health/live` | `http://localhost:5103/health/ready` |
| InventoryService | `http://localhost:5101/health/live` | `http://localhost:5101/health/ready` |
| ShipmentService | `http://localhost:5102/health/live` | `http://localhost:5102/health/ready` |

The existing `/health` endpoint remains available and behaves like readiness. Liveness is process-only and does not depend on SQL, RabbitMQ, or Redis. CompanyService readiness verifies CompanyDb connectivity. InventoryService and ShipmentService readiness checks verify RabbitMQ connectivity by opening a connection and channel. They do not validate every exchange, queue, or binding.

Redis is available in Docker Compose. CompanyService uses it as a cache for company address detail lookups, but current application health checks do not depend on Redis. Check it directly with:

```powershell
docker compose exec redis redis-cli ping
```

## Correlation IDs

HTTP responses include an `X-Correlation-ID` header. Send the same header on requests to follow related Gateway and service logs. RabbitMQ message-level correlation is not implemented yet; `EventId` still identifies individual integration events.

## RabbitMQ Queues And DLQs

Consumers declare their queues, bindings, and dead-letter queues when they connect. Failed consumer messages are retried briefly, then nacked with `requeue: false` so they can be routed to the corresponding DLQ.

Useful checks in RabbitMQ management tooling:

- consumer queues have active consumers
- DLQs contain messages that failed processing
- routing keys match the expected stock reservation event flow

If RabbitMQ durable queues already exist with older arguments, RabbitMQ may reject redeclaration. In local development, inspect the queue declaration error and clean up the conflicting local queue manually if needed.

## Outbox Failures

ShipmentService and InventoryService publish integration events through outbox tables. A publish failure should not lose the event immediately:

- retry metadata is updated
- `retry_count` increases
- `error` records the latest failure
- messages that keep failing can move to a poison state through `failed_at_utc`

Outbox publishers mark a message as processed only after RabbitMQ confirms the publish. Publishing also uses mandatory routing, so unroutable messages fail the publish and remain retryable in the outbox. This improves broker acknowledgement and routing safety, but the design is still at-least-once rather than exactly-once. A service can still publish an event and crash before marking the outbox row as processed, so duplicate delivery remains possible and consumers must stay idempotent.

Use application logs together with the relevant outbox rows to understand whether a message is waiting, retrying, processed, or permanently failed.

## Duplicate Events

The services use inbox tables for idempotency. Duplicate `EventId` deliveries are expected in an at-least-once messaging design and should be ignored safely.

Shipment reservation result handlers also guard shipment state. Stale or conflicting reservation result events are recorded in the inbox but do not overwrite shipments that are no longer `ReservationRequested`.

## Database Rule

EF Core migrations are intentionally not used. Database changes should be handled with manual SQL outside EF migrations.

Docker Compose does not start SQL Server and does not create or recreate database schema automatically. Prepare `CompanyDb`, `InventoryDb`, and `ShipmentDb` manually on the host `SQLEXPRESS` instance from the checked-in SQL scripts. `bootstrap-docker-sql.ps1` is a legacy helper for the previous Docker SQL Server setup and is not part of the current Compose flow.

For database connection failures, verify:

- the `SQLEXPRESS` Windows service is running
- TCP/IP is enabled on fixed port `14330`
- Docker can reach `host.docker.internal:14330`
- the required databases exist
- service SQL logins and database users exist
- the local root `.env` file contains `COMPANYSERVICE_DB_PASSWORD`, `INVENTORYSERVICE_DB_PASSWORD`, and `SHIPMENTSERVICE_DB_PASSWORD`

Useful checks:

```powershell
Test-NetConnection localhost -Port 14330
docker run --rm alpine:3.20 sh -c "nc -vz host.docker.internal 14330"
docker compose logs companyservice
docker compose logs inventoryservice
docker compose logs shipmentservice
```

CompanyService uses `CompanyDb` for health checks and minimal Company/Address CRUD.
ShipmentService uses CompanyService when creating shipments because sender/receiver company/address IDs are required. If CompanyService is unavailable during that validation, shipment creation fails with a service dependency error.
