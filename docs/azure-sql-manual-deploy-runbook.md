# Azure SQL Manual Deployment Runbook

This runbook describes the production-style database deployment process for LogisticsHub on Azure SQL Database. EF Core migrations are intentionally not used in this repository. Schema changes must be prepared, reviewed, and applied as manual SQL scripts.

## Scope

LogisticsHub uses one database per service boundary:

| Service | Database | Ownership |
|---|---|---|
| CompanyService | `CompanyDb` | Company and company address schema. |
| InventoryService | `InventoryDb` | Inventory, stock reservation, inbox, and outbox schema. |
| ShipmentService | `ShipmentDb` | Shipment, shipment item, inbox, and outbox schema. |

Shared infrastructure must not silently mutate service-owned schemas. Cross-service references should stay explicit in application logic or integration events; do not add cross-database foreign keys as a shortcut.

## Script Organization

Current schema snapshots and compatibility patches live at the repository root and are described in [Database schema](database-schema.md). Future deployment scripts should be added as reviewed manual SQL under:

```text
sql/manual/<service>/<yyyyMMddHHmm>-<short-change-name>.sql
```

Use boring service folder names such as `company`, `inventory`, and `shipment`. Keep each script scoped to one service database unless a coordinated release truly needs multiple scripts.

## Manual Deployment Flow

1. Prepare the SQL script from the approved application/schema change.
2. Review the script directly, including expected tables, indexes, constraints, default values, and data backfill behavior.
3. Confirm the target Azure SQL server and database name before connecting.
4. Take a backup, export, or Azure restore point appropriate for the database and change risk.
5. Apply the script to the intended Azure SQL database with an approved SQL tool such as Azure Data Studio, `sqlcmd`, or a controlled release step.
6. Verify the expected schema objects after the script completes.
7. Verify service startup and readiness for the affected workload through the deployed service health checks.
8. Check inbox/outbox assumptions for affected services, especially required columns, indexes, retry columns, and poison/failure state columns.
9. Record the applied script name, target database, operator, timestamp, and release/change reference in the release notes or operations log.

## Safety Checks

- Confirm target server and database before executing any script.
- Use transactions where safe. Avoid wrapping long-running index rebuilds, large backfills, or operations that Azure SQL cannot safely roll back in one transaction.
- Prefer idempotent scripts where practical, especially for additive objects and compatibility patches.
- Destructive changes require an explicit backup or restore plan before execution.
- Verify permissions are limited to what the deployment operator or automation needs for schema changes.
- Never commit production connection strings, SQL passwords, Entra values, or private server names.
- Keep local SQL Express values local-only. Azure SQL connection strings should come from secret management and environment/configuration at deployment time.

## Drift And Verification

Before applying a script, compare the intended change with the current checked-in schema snapshot and the live database where possible. After applying a script, verify:

- expected tables, columns, indexes, constraints, and default values;
- `inventory_inbox_messages`, `inventory_outbox_messages`, `shipment_inbox_messages`, and `shipment_outbox_messages` columns required by consumers and outbox processors;
- CompanyService `companies` and `company_addresses` constraints if CompanyDb changed;
- affected service readiness at `/health/ready`;
- application logs for failed startup, SQL exceptions, or unexpected outbox/inbox processing errors.

Refresh local schema snapshots with `export-local-db-schema.ps1` when a manual change should become the documented local baseline.

## Rollback Expectations

Rollback must be planned per script. Additive changes may have a simple rollback script, but destructive or data-changing scripts should normally rely on restore point, backup, or a carefully reviewed forward-fix plan. Do not assume every schema change is safely reversible.

## Azure Deployment Notes

For AKS, database connection strings should be supplied through Kubernetes secrets, Key Vault integration, or the chosen secret-management mechanism. The application should receive them through ASP.NET Core configuration keys such as `ConnectionStrings__CompanyDb`, `ConnectionStrings__InventoryDb`, and `ConnectionStrings__ShipmentDb`.

This runbook does not create Azure SQL resources, AKS manifests, CI/CD workflows, or secrets. Those remain separate deployment work.
