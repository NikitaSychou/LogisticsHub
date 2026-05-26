# Database Schema

LogisticsHub uses one SQL Server database per service:

- `InventoryDb`
- `ShipmentDb`
- `CompanyDb`

The current local schema was exported from `localhost\SQLEXPRESS` with Windows Authentication by running:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\export-local-db-schema.ps1
```

The export helper is schema-only. It does not script table data and does not modify the databases. The generated schema snapshots are:

- `InventoryDb.schema.sql`
- `ShipmentDb.schema.sql`
- `CompanyDb.schema.sql`

`ShipmentDb.company-address-columns.sql` is an idempotent manual patch for existing local Docker `ShipmentDb` databases. It adds nullable sender/receiver company and address reference columns without inserting data or adding cross-database foreign keys.

`InventoryDb.schema.sql` and `ShipmentDb.schema.sql` were exported from local SQL Express. `CompanyDb.schema.sql` is the manual baseline used by CompanyService persistence wiring and health checks.

EF Core migrations are intentionally not used in this repository. Database schema changes should be made with manual SQL. Re-export existing local SQL Express schemas with the helper script where applicable, and review any manual snapshots directly.

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

`dbo.shipments` also contains nullable future-reference columns:

- `sender_company_id`
- `sender_address_id`
- `receiver_company_id`
- `receiver_address_id`

These columns are manual schema preparation for a later ShipmentService API/domain update. They are intentionally nullable, are not mapped by the current ShipmentService EF model, and do not have foreign keys to `CompanyDb` because service databases remain independent.

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

## CompanyDb

`CompanyDb` is the manual schema baseline for the CompanyService data boundary. CompanyService connects to this database for health checks and minimal Company/Address CRUD.

The baseline contains:

| Table | Purpose |
|---|---|
| `dbo.companies` | Future company master records. |
| `dbo.company_addresses` | Future typed addresses owned by a company. |

Important manual constraints and indexes:

- `CK_companies_status` allows only `Active` and `Inactive`.
- `CK_company_addresses_address_type` allows only `Legal`, `Billing`, `Shipping`, and `Warehouse`.
- `FK_company_addresses_companies` enforces address ownership by company.
- `CK_company_addresses_country_code_length` requires two-character country codes.
- Check constraints prevent empty `companies.name`, `company_addresses.city`, and `company_addresses.line1`.
- `IX_Companies_Name` supports company-name lookup.
- `UX_Companies_ExternalCode` enforces unique non-null external company codes.
- `IX_CompanyAddresses_CompanyId` and `IX_CompanyAddresses_AddressType` support address lookup by owner and type.

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
| Future shipment references | `dbo.shipments` contains nullable sender/receiver company and address reference columns that are not mapped by current ShipmentService code. | No current runtime dependency; later ShipmentService work can map and validate these values through CompanyService. |
| Delete behavior | EF configures cascade delete for `stock_reservation_items` -> `stock_reservations` and `shipment_items` -> `shipments`, but the exported FKs do not include `ON DELETE CASCADE`. | No current application delete workflow depends on this, but it is a schema/model mismatch. |
| Inventory outbox polling index | ShipmentDb has an unprocessed outbox index; InventoryDb does not. | Not a functional blocker; possible future performance gap. |
| CompanyDb code mapping | `CompanyDb.schema.sql` has CompanyService domain entities, EF mappings, and minimal CRUD endpoints. | Manual SQL remains the source of truth; no EF migrations are used. |

## Local Smoke Testing

For the full local flow, the databases must exist before the services run:

- `InventoryDb`
- `ShipmentDb`

`CompanyDb` can also be bootstrapped locally. It is required for CompanyService health, but it is not used by the current inventory-to-shipment smoke-test flow.

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
- `CompanyDb.schema.sql`

When updating an already-created Docker `ShipmentDb`, the bootstrap helper also applies:

- `ShipmentDb.company-address-columns.sql`

Do not use EF Core migrations for this project.
