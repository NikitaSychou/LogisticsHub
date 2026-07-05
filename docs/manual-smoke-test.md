# Manual Smoke Test

This guide verifies the smallest full local LogisticsHub flow through the Gateway:

```text
create inventory item
  -> create company/address references
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
- SQL Server with `InventoryDb`, `ShipmentDb`, and `CompanyDb` prepared
- RabbitMQ
- Redis
- Gateway, CompanyService, InventoryService, and ShipmentService running in Development
- For Docker Compose: a local root `.env` file with `COMPANYSERVICE_DB_PASSWORD`, `INVENTORYSERVICE_DB_PASSWORD`, and `SHIPMENTSERVICE_DB_PASSWORD`

The checked-in local `appsettings.json` files point to SQL Server Express:

```text
localhost\SQLEXPRESS
```

with Windows Authentication. Docker Compose overrides application connection strings so containers connect to the host `SQLEXPRESS` instance through `host.docker.internal,14330`.

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

Prepare host `SQLEXPRESS` first:

1. Enable SQL Server TCP/IP on fixed port `14330`.
2. Create `CompanyDb`, `InventoryDb`, and `ShipmentDb`.
3. Apply the checked-in manual SQL schemas and patches described in [Database schema](database-schema.md).
4. Create the local root `.env` file with the three service database password variables.

From the repository root:

```powershell
docker compose up --build
```

Compose app containers do not use `localhost\SQLEXPRESS`; they use the Compose connection strings that point to `host.docker.internal,14330`.

Compose exposes:

| Component | URL |
|---|---|
| Gateway | `http://localhost:5100` |
| CompanyService | `http://localhost:5103` |
| InventoryService | `http://localhost:5101` |
| ShipmentService | `http://localhost:5102` |
| RabbitMQ Management | `http://localhost:15672` |
| SQL Server from containers | `host.docker.internal,14330` |

RabbitMQ Management uses the local default credentials:

```text
guest / guest
```

## Verify Health

```powershell
Invoke-RestMethod http://localhost:5100/health
Invoke-RestMethod http://localhost:5100/health/live
Invoke-RestMethod http://localhost:5100/health/ready
Invoke-RestMethod http://localhost:5100/company/health
Invoke-RestMethod http://localhost:5103/health
Invoke-RestMethod http://localhost:5103/health/live
Invoke-RestMethod http://localhost:5103/health/ready
Invoke-RestMethod http://localhost:5100/shipment/health
Invoke-RestMethod http://localhost:5101/health
Invoke-RestMethod http://localhost:5101/health/live
Invoke-RestMethod http://localhost:5101/health/ready
Invoke-RestMethod http://localhost:5102/health
Invoke-RestMethod http://localhost:5102/health/live
Invoke-RestMethod http://localhost:5102/health/ready
```

Expected result for each service is `Healthy`.

CompanyService is part of shipment creation because ShipmentService validates required sender/receiver company/address references through CompanyService. Liveness endpoints are process-only. The existing `/health` endpoints behave like readiness. CompanyService readiness verifies CompanyDb connectivity. InventoryService and ShipmentService readiness checks verify their SQL database and RabbitMQ connectivity. They do not prove that the full SQL schema exists.

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

## Create Company And Addresses

Shipment creation requires sender and receiver company/address references. This guide creates separate sender and receiver companies so the request mirrors the real boundary more clearly.

```powershell
$senderCompanyRequest = @{
    name = "Manual Smoke Sender $Sku"
    externalCode = "SMOKE-SENDER-$Sku"
    status = "Active"
} | ConvertTo-Json

$senderCompany = Invoke-RestMethod `
    -Method Post `
    -Uri "$Gateway/company/companies" `
    -ContentType "application/json" `
    -Headers @{ "X-Correlation-ID" = $CorrelationId } `
    -Body $senderCompanyRequest

$senderAddressRequest = @{
    addressType = "Warehouse"
    countryCode = "US"
    city = "Seattle"
    postalCode = "98101"
    line1 = "100 Smoke Sender Way"
    line2 = $null
} | ConvertTo-Json

$senderAddress = Invoke-RestMethod `
    -Method Post `
    -Uri "$Gateway/company/companies/$($senderCompany.id)/addresses" `
    -ContentType "application/json" `
    -Headers @{ "X-Correlation-ID" = $CorrelationId } `
    -Body $senderAddressRequest

$receiverCompanyRequest = @{
    name = "Manual Smoke Receiver $Sku"
    externalCode = "SMOKE-RECEIVER-$Sku"
    status = "Active"
} | ConvertTo-Json

$receiverCompany = Invoke-RestMethod `
    -Method Post `
    -Uri "$Gateway/company/companies" `
    -ContentType "application/json" `
    -Headers @{ "X-Correlation-ID" = $CorrelationId } `
    -Body $receiverCompanyRequest

$receiverAddressRequest = @{
    addressType = "Shipping"
    countryCode = "US"
    city = "Portland"
    postalCode = "97201"
    line1 = "200 Smoke Receiver Ave"
    line2 = $null
} | ConvertTo-Json

$receiverAddress = Invoke-RestMethod `
    -Method Post `
    -Uri "$Gateway/company/companies/$($receiverCompany.id)/addresses" `
    -ContentType "application/json" `
    -Headers @{ "X-Correlation-ID" = $CorrelationId } `
    -Body $receiverAddressRequest
```

Optionally verify the sender address detail endpoint twice. The second call should be able to use CompanyService's Redis cache while returning the same API response:

```powershell
$senderAddressDetailUri = "$Gateway/company/companies/$($senderCompany.id)/addresses/$($senderAddress.id)"

$senderAddressDetailFirst = Invoke-RestMethod `
    -Method Get `
    -Uri $senderAddressDetailUri `
    -Headers @{ "X-Correlation-ID" = $CorrelationId }

$senderAddressDetailSecond = Invoke-RestMethod `
    -Method Get `
    -Uri $senderAddressDetailUri `
    -Headers @{ "X-Correlation-ID" = $CorrelationId }

$senderAddressDetailFirst
$senderAddressDetailSecond
```

If running through Docker Compose, inspect the Redis key and TTL:

```powershell
$senderAddressCacheKey = "company-address:$($senderCompany.id):$($senderAddress.id)"
docker compose exec redis redis-cli EXISTS $senderAddressCacheKey
docker compose exec redis redis-cli TTL $senderAddressCacheKey
```

Expected `EXISTS` result is `1`. Expected TTL is close to `86400` seconds after the first detail lookup.

## Create Shipment

The Gateway route `/shipment/{**catch-all}` strips `/shipment` and forwards to ShipmentService.

```powershell
$shipmentRequest = @{
    senderCompanyId = $senderCompany.id
    senderAddressId = $senderAddress.id
    receiverCompanyId = $receiverCompany.id
    receiverAddressId = $receiverAddress.id
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
  "status": "ReservationRequested",
  "senderCompanyId": "...",
  "senderAddressId": "...",
  "receiverCompanyId": "...",
  "receiverAddressId": "..."
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
CompanyService: http://localhost:5103
InventoryService: http://localhost:5101
ShipmentService: http://localhost:5102
```

For example:

```powershell
Invoke-RestMethod "http://localhost:5103/companies/$($senderCompany.id)/addresses/$($senderAddress.id)"
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
```

To follow only the application services:

```powershell
docker compose logs -f gateway companyservice inventoryservice shipmentservice
```

Use the same `X-Correlation-ID` on HTTP requests to connect Gateway and service logs. RabbitMQ messages use `EventId` for event-level tracking.

## Common Failures

| Symptom | Inspect |
|---|---|
| Readiness endpoint is not `Healthy` | RabbitMQ or CompanyDb availability, service logs, container status. |
| Service exits on startup | SQL connection string, missing database schema, RabbitMQ connection settings. |
| `POST /inventory/inventory-items` returns `500 Internal Server Error` with SQL error 4060 | Host `SQLEXPRESS` is missing `InventoryDb` or the service login cannot access it. |
| `POST /inventory/inventory-items` returns `409 Conflict` | The SKU already exists; run again with a fresh SKU. |
| `POST /shipment/shipments` returns `400 Bad Request` | Request must include all sender/receiver company/address IDs, at least one item, required SKU values, positive quantities, and no duplicate SKUs. |
| `POST /shipment/shipments` returns `503 Service Unavailable` | ShipmentService could not validate sender/receiver references through CompanyService; check CompanyService health and logs. |
| Shipment stays `ReservationRequested` | Check ShipmentService outbox logs, RabbitMQ queues, InventoryService consumer logs, and InventoryService outbox logs. |
| Shipment becomes `ReservationFailed` | Check `reservationFailureReason`, inventory item existence, stock balance, and available quantity. |
| RabbitMQ queue declaration fails | Existing local durable queue may have incompatible arguments; inspect RabbitMQ logs and management UI. |
| DLQ contains messages | Check service logs for deserialization or handler failures, then inspect the corresponding `.dlq`. |

## Stop The Local System

```powershell
docker compose down
```

This stops containers but keeps named volumes unless you remove them explicitly.
