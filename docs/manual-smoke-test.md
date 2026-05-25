# Manual Smoke Test

This guide verifies the smallest full local LogisticsHub flow through the Gateway:

```text
create inventory item
  -> create shipment
  -> ShipmentService outbox
  -> RabbitMQ
  -> InventoryService stock reservation
  -> InventoryService outbox
  -> ShipmentService reservation result
```

Use Gateway endpoints for the smoke-test path. Direct service URLs are included only for diagnostics.

## Prerequisites

- .NET SDK 10
- SQL Server with `InventoryDb` and `ShipmentDb` prepared
- RabbitMQ
- Gateway, InventoryService, and ShipmentService running in Development

The checked-in local `appsettings.json` files point to SQL Server Express:

```text
localhost\SQLEXPRESS
```

with Windows Authentication. Docker Compose overrides database and RabbitMQ settings for containers.

Database schema is not created automatically and EF Core migrations are intentionally not used. See [Database schema](database-schema.md) before running the full flow.

## Choose One Runtime Path

Use one consistent database/runtime path for the whole smoke test.

### Option A: Host Services With SQL Express

Use this path when the prepared local databases are in `localhost\SQLEXPRESS`.

Start SQL Server Express and RabbitMQ first. RabbitMQ can be local or from Compose:

```powershell
docker compose up rabbitmq
```

Then run the services directly from separate terminals:

```powershell
dotnet run --project .\src\InventoryService\LogisticsHub.InventoryService\LogisticsHub.InventoryService.csproj
dotnet run --project .\src\ShipmentService\LogisticsHub.ShipmentService\LogisticsHub.ShipmentService.csproj
dotnet run --project .\src\Gateway\LogisticsHub.Gateway\LogisticsHub.Gateway.csproj
```

The checked-in local service settings use:

```text
SQL Server: localhost\SQLEXPRESS
RabbitMQ: localhost:5672
```

### Option B: Full Docker Compose

From the repository root:

```powershell
docker compose up --build
```

Compose app containers do not use `localhost\SQLEXPRESS`; they use the Compose connection strings that point to `sqlserver,1433` with the `sa` login.

Compose exposes:

| Component | URL |
|---|---|
| Gateway | `http://localhost:5100` |
| CompanyService | `http://localhost:5103` |
| InventoryService | `http://localhost:5101` |
| ShipmentService | `http://localhost:5102` |
| RabbitMQ Management | `http://localhost:15672` |
| SQL Server container | `localhost,1433` |

After SQL Server is running, bootstrap the container databases from the checked-in schema snapshots:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\bootstrap-docker-sql.ps1
```

The script creates `InventoryDb` and `ShipmentDb` in the `logisticshub-sqlserver` container if they do not exist, then applies:

- `InventoryDb.schema.sql`
- `ShipmentDb.schema.sql`

It does not insert seed or business data. If a database already contains a partial or unexpected schema, the script stops and leaves it unchanged.

RabbitMQ Management uses the local default credentials:

```text
guest / guest
```

## Verify Health

```powershell
Invoke-RestMethod http://localhost:5100/health
Invoke-RestMethod http://localhost:5103/health
Invoke-RestMethod http://localhost:5101/health
Invoke-RestMethod http://localhost:5102/health
```

Expected result for each service is `Healthy`.

CompanyService currently exposes only shell endpoints and is not part of this business smoke-test path. InventoryService and ShipmentService health checks verify RabbitMQ connectivity. They do not prove that the full SQL schema exists.

## Smoke-Test Values

Use a fresh SKU for each run to avoid `409 Conflict` from duplicate inventory items.

```powershell
$Gateway = "http://localhost:5100"
$Sku = "SMOKE-SKU-$(Get-Date -Format yyyyMMddHHmmss)"
$CorrelationId = "manual-smoke-$(Get-Date -Format yyyyMMddHHmmss)"
```

## Create Inventory Data

The Gateway route `/inventory/{**catch-all}` strips `/inventory` and forwards to InventoryService.

```powershell
$inventoryRequest = @{
    sku = $Sku
    name = "Manual Smoke Test Item"
    quantityAvailable = 10
} | ConvertTo-Json

Invoke-RestMethod `
    -Method Post `
    -Uri "$Gateway/inventory/inventory-items" `
    -ContentType "application/json" `
    -Headers @{ "X-Correlation-ID" = $CorrelationId } `
    -Body $inventoryRequest
```

Expected response:

```json
{
  "sku": "SMOKE-SKU-...",
  "name": "Manual Smoke Test Item",
  "quantityAvailable": 10
}
```

## Create Shipment

The Gateway route `/shipment/{**catch-all}` strips `/shipment` and forwards to ShipmentService.

```powershell
$shipmentRequest = @{
    items = @(
        @{
            sku = $Sku
            quantity = 2
        }
    )
} | ConvertTo-Json -Depth 5

$createdShipment = Invoke-RestMethod `
    -Method Post `
    -Uri "$Gateway/shipment/shipments" `
    -ContentType "application/json" `
    -Headers @{ "X-Correlation-ID" = $CorrelationId } `
    -Body $shipmentRequest

$createdShipment
$ShipmentId = $createdShipment.shipmentId
```

Expected initial response:

```json
{
  "shipmentId": "...",
  "status": "ReservationRequested"
}
```

Shipment creation writes a ShipmentService outbox row. It does not mean stock reservation has completed yet.

## Poll Shipment Status

The outbox publishers poll every few seconds, and the result depends on RabbitMQ delivery plus InventoryService processing. Poll until the shipment leaves `ReservationRequested`.

```powershell
$finalShipment = $null

for ($attempt = 1; $attempt -le 20; $attempt++) {
    $shipment = Invoke-RestMethod `
        -Method Get `
        -Uri "$Gateway/shipment/shipments/$ShipmentId" `
        -Headers @{ "X-Correlation-ID" = $CorrelationId }

    "Attempt $attempt: $($shipment.status)"

    if ($shipment.status -ne "ReservationRequested") {
        $finalShipment = $shipment
        break
    }

    Start-Sleep -Seconds 2
}

$finalShipment
```

Expected successful final status:

```text
Reserved
```

The final shipment response should include a non-null `reservationId`.

If the final status is `ReservationFailed`, inspect `reservationFailureReason`. Common causes are missing SKU, inactive SKU, missing stock balance, or insufficient stock.

## Optional Inventory Checks

Check the created inventory item through Gateway:

```powershell
Invoke-RestMethod "$Gateway/inventory/inventory-items/$Sku"
```

After a successful reservation of quantity `2` from starting stock `10`, expected `quantityAvailable` is `8`.

If the final shipment has a `reservationId`, check the reservation directly:

```powershell
Invoke-RestMethod "$Gateway/inventory/stock-reservations/$($finalShipment.reservationId)"
```

Expected reservation status:

```text
Active
```

Direct diagnostic URLs:

```text
InventoryService: http://localhost:5101
ShipmentService: http://localhost:5102
```

For example:

```powershell
Invoke-RestMethod "http://localhost:5101/inventory-items/$Sku"
Invoke-RestMethod "http://localhost:5102/shipments/$ShipmentId"
```

## RabbitMQ Checks

Open RabbitMQ Management:

```text
http://localhost:15672
```

Expected exchange:

```text
logisticshub.events
```

Expected routing keys:

```text
stock-reservation.requested
stock-reservation.reserved
stock-reservation.failed
```

Expected queues:

```text
inventory.stock-reservation.requested
shipment.stock-reservation.reserved
shipment.stock-reservation.failed
```

Each consumer also declares a matching `.dlq` dead-letter queue. During a successful smoke test, the main queues should drain and DLQs should stay empty.

## Useful Logs

```powershell
docker compose ps
docker compose logs gateway
docker compose logs companyservice
docker compose logs inventoryservice
docker compose logs shipmentservice
docker compose logs rabbitmq
docker compose logs sqlserver
```

To follow only the application services:

```powershell
docker compose logs -f gateway inventoryservice shipmentservice
```

Use the same `X-Correlation-ID` on HTTP requests to connect Gateway and service logs. RabbitMQ messages use `EventId` for event-level tracking.

## Common Failures

| Symptom | Inspect |
|---|---|
| Health endpoint is not `Healthy` | RabbitMQ availability, service logs, container status. |
| Service exits on startup | SQL connection string, missing database schema, RabbitMQ connection settings. |
| `POST /inventory/inventory-items` returns `500 Internal Server Error` with SQL error 4060 | The Docker SQL Server container is missing `InventoryDb`; run `.\bootstrap-docker-sql.ps1` after SQL Server is running. |
| `POST /inventory/inventory-items` returns `409 Conflict` | The SKU already exists; run again with a fresh SKU. |
| `POST /shipment/shipments` returns `400 Bad Request` | Request must include at least one item; SKU is required; quantity must be greater than zero; duplicate SKUs are rejected. |
| Shipment stays `ReservationRequested` | Check ShipmentService outbox logs, RabbitMQ queues, InventoryService consumer logs, and InventoryService outbox logs. |
| Shipment becomes `ReservationFailed` | Check `reservationFailureReason`, inventory item existence, stock balance, and available quantity. |
| RabbitMQ queue declaration fails | Existing local durable queue may have incompatible arguments; inspect RabbitMQ logs and management UI. |
| DLQ contains messages | Check service logs for deserialization or handler failures, then inspect the corresponding `.dlq`. |

## Stop The Local System

```powershell
docker compose down
```

This stops containers but keeps named volumes unless you remove them explicitly.
