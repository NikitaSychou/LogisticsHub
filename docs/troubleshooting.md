# Troubleshooting

This project uses local SQL Server, RabbitMQ, Redis, Gateway, CompanyService, InventoryService, and ShipmentService. Start with the smallest check that proves which part is failing.

## Startup Checks

1. Confirm SQL Server is running and the configured connection strings are reachable.
2. Confirm RabbitMQ is running on the configured host and port.
3. Start InventoryService and ShipmentService before the Gateway.
4. Check each service console for configuration, database, or RabbitMQ connection errors.

If using Docker Compose, check container status with `docker compose ps`. RabbitMQ management is available at `http://localhost:15672` with the local development credentials from the container image defaults. SQL Server should show as running before the services try to connect.

`depends_on` starts containers in order but does not guarantee that SQL Server or RabbitMQ are fully ready. If an app container exits during startup, check logs with:

```powershell
docker compose logs inventoryservice
docker compose logs shipmentservice
docker compose logs companyservice
docker compose logs gateway
```

## Health Endpoints

| Service | Health endpoint |
|---|---|
| Gateway | `http://localhost:5100/health` |
| CompanyService | `http://localhost:5103/health` |
| InventoryService | `http://localhost:5101/health` |
| ShipmentService | `http://localhost:5102/health` |

InventoryService and ShipmentService health checks verify RabbitMQ connectivity by opening a connection and channel. They do not validate every exchange, queue, or binding.

Redis is available in Docker Compose for local infrastructure work, but current application health checks do not depend on Redis. Check it directly with:

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

Docker Compose does not create or recreate database schema. Prepare `InventoryDb` and `ShipmentDb` manually before testing the full app flow.
