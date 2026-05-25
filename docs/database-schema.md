# Database Schema

LogisticsHub uses one SQL Server database per service:

- `InventoryDb`
- `ShipmentDb`

The current local schema was exported from `localhost\SQLEXPRESS` with Windows Authentication by running:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\export-local-db-schema.ps1
```

The export helper is schema-only. It does not script table data and does not modify the databases. The generated schema snapshots are:

- `InventoryDb.schema.sql`
- `ShipmentDb.schema.sql`

EF Core migrations are intentionally not used in this repository. Database schema changes should be made with manual SQL and then re-exported with the helper script.

## InventoryDb

The code expects these mapped tables:

| Table | Purpose |
|---|---|
| `dbo.items` | Inventory item master data. |
| `dbo.stock_balances` | On-hand and reserved quantities per item. |
| `dbo.stock_reservations` | Reservation header per shipment. |
| `dbo.stock_reservation_items` | Reserved item quantities. |
| `dbo.inventory_inbox_messages` | Idempotency records for consumed integration events. |
| `dbo.inventory_outbox_messages` | Retryable integration events published by InventoryService. |

Current exported schema includes all mapped InventoryService tables and all columns required by the EF mappings and raw outbox claiming SQL.

Important exported constraints and indexes:

- `stock_balances.row_version` is `timestamp`/rowversion and satisfies the EF concurrency token.
- `IX_inventory_inbox_messages_event_id` is a unique index and is required for duplicate event handling.
- `UQ_items_sku` enforces one item per SKU. This supports the application invariant but is not configured in EF.
- `UQ_stock_reservations_shipment_id` enforces one reservation per shipment. This supports the current workflow but is not configured in EF.
- Check constraints enforce non-negative stock quantities, reservation item quantity greater than zero, and allowed reservation statuses. These are database-side guards and are not configured in EF.
- Default constraints exist for several IDs and timestamps. The application usually supplies these values directly, so these defaults are fallback behavior.

Outbox compatibility:

- `inventory_outbox_messages` contains `processed_at_utc`, `failed_at_utc`, `next_attempt_at_utc`, `locked_at_utc`, `locked_by`, and `occurred_at_utc`, which are required by the raw row-claiming SQL in `InventoryDbContext`.
- No filtered or covering unprocessed-outbox index is present in the exported InventoryDb schema. This is not a functional mismatch, but it may become a performance issue if local outbox volume grows.

## ShipmentDb

The code expects these mapped tables:

| Table | Purpose |
|---|---|
| `dbo.shipments` | Shipment header and reservation status. |
| `dbo.shipment_items` | Shipment item lines. |
| `dbo.shipment_inbox_messages` | Idempotency records for consumed integration events. |
| `dbo.shipment_outbox_messages` | Retryable integration events published by ShipmentService. |

Current exported schema includes all mapped ShipmentService tables and all columns required by the EF mappings and raw outbox claiming SQL.

Important exported constraints and indexes:

- `IX_shipment_inbox_messages_event_id` is a unique index and is required for duplicate event handling.
- `IX_shipment_outbox_messages_unprocessed` supports polling unprocessed outbox rows.
- `UQ_shipments_shipment_number` enforces unique shipment numbers. This supports the generated shipment-number invariant but is not configured in EF.
- `IX_shipments_reservation_id_not_null` enforces unique non-null reservation IDs. This supports the current workflow but is not configured in EF.
- Check constraints enforce positive shipment item quantities and allowed shipment statuses. These are database-side guards and are not configured in EF.
- Default constraints exist for several IDs and timestamps. The application usually supplies these values directly, so these defaults are fallback behavior.

The exported schema also contains `dbo.shipment_status_history`. This table is not mapped by the current ShipmentService EF model and is not used by the current application code.

Outbox compatibility:

- `shipment_outbox_messages` contains `processed_at_utc`, `failed_at_utc`, `next_attempt_at_utc`, `locked_at_utc`, `locked_by`, and `occurred_at_utc`, which are required by the raw row-claiming SQL in `ShipmentDbContext`.

## Comparison Notes

The exported local SQL Express schema is usable for the current local smoke-test path, but it is not a pure EF-generated schema. The database contains deliberate manual SQL details that are not represented in the EF mappings.

Known differences between exported schema and EF mappings:

| Area | Difference | Impact |
|---|---|---|
| Date precision | Several domain tables use `datetime2(3)` while EF conventions would normally map `DateTime` to `datetime2(7)` unless precision is configured. | Usually safe for local workflow, but the precision should be documented in manual SQL. |
| Shipment string lengths | Shipment tables use bounded lengths such as `nvarchar(50)`, `nvarchar(64)`, `nvarchar(200)`, `nvarchar(500)`, and `nvarchar(1000)`, while EF mappings do not configure these lengths. | Long API input can fail at database save time unless validation or EF length mappings are added later. |
| Additional unique indexes | Several uniqueness rules exist only in SQL, including item SKU, shipment number, stock reservation shipment ID, and non-null shipment reservation ID. | Useful invariants, but not visible from EF configuration alone. |
| Additional check constraints | Quantity and status checks exist only in SQL. | Useful database guards, but not visible from EF configuration alone. |
| Default constraints | ID and timestamp defaults exist only in SQL. | Usually fallback behavior because the application supplies values. |
| Extra table | `dbo.shipment_status_history` exists in `ShipmentDb` but is not mapped by current code. | No current runtime dependency; keep documented as local schema state. |
| Delete behavior | EF configures cascade delete for `stock_reservation_items` -> `stock_reservations` and `shipment_items` -> `shipments`, but the exported FKs do not include `ON DELETE CASCADE`. | No current application delete workflow depends on this, but it is a schema/model mismatch. |
| Inventory outbox polling index | ShipmentDb has an unprocessed outbox index; InventoryDb does not. | Not a functional blocker; possible future performance gap. |

## Local Smoke Testing

For the full local flow, the databases must exist before the services run:

- `InventoryDb`
- `ShipmentDb`

Inventory seed data does not need to be inserted manually after the schema exists. Use the Inventory API to create an item and starting stock:

```http
POST /inventory/inventory-items
```

Then create a shipment through the Gateway:

```http
POST /shipment/shipments
```

The async flow uses ShipmentService outbox, RabbitMQ, InventoryService reservation processing, InventoryService outbox, and ShipmentService reservation-result consumers.

## Refreshing Schema Snapshots

After manual SQL schema changes, regenerate the local schema snapshots from the repository root:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\export-local-db-schema.ps1
```

Review changes to:

- `InventoryDb.schema.sql`
- `ShipmentDb.schema.sql`

Do not use EF Core migrations for this project.
