import { InventoryItemRow, InventoryItemsPageResult } from '../models/inventory.models';

export function toInventoryItemsPageResult(payload: unknown, requestedPage: number): InventoryItemsPageResult {
  const record = asRecord(payload);
  const rawItems = Array.isArray(record['items']) ? record['items'] : [];

  return {
    items: rawItems.map(toInventoryItemRow),
    pageNumber: numberValue(record, 'pageNumber') ?? requestedPage,
    pageSize: numberValue(record, 'pageSize') ?? rawItems.length,
    hasMore: booleanValue(record, 'hasMore') ?? false,
  };
}

export function toInventoryItemRow(payload: unknown): InventoryItemRow {
  const record = asRecord(payload);

  return {
    sku: stringValue(record, ['sku']),
    name: stringValue(record, ['name']),
    quantityAvailable: numberValue(record, 'quantityAvailable'),
    raw: payload,
  };
}

function asRecord(value: unknown): Record<string, unknown> {
  return value !== null && typeof value === 'object' ? (value as Record<string, unknown>) : {};
}

function stringValue(record: Record<string, unknown>, keys: string[]): string | undefined {
  for (const key of keys) {
    const value = record[key];
    if (typeof value === 'string' && value.trim().length > 0) {
      return value;
    }
  }

  return undefined;
}

function numberValue(record: Record<string, unknown>, key: string): number | undefined {
  const value = record[key];
  return typeof value === 'number' && Number.isFinite(value) ? value : undefined;
}

function booleanValue(record: Record<string, unknown>, key: string): boolean | undefined {
  const value = record[key];
  return typeof value === 'boolean' ? value : undefined;
}
