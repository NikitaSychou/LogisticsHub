import { CompanyAddressRow, CompanyPageResult, CompanyRow } from '../models/company.models';

export function toCompanyPageResult(payload: unknown, requestedPage: number): CompanyPageResult {
  const record = asRecord(payload);
  const rawItems = Array.isArray(record['items']) ? record['items'] : [];

  return {
    items: rawItems.map(toCompanyRow),
    pageNumber: numberValue(record, 'pageNumber') ?? requestedPage,
    pageSize: numberValue(record, 'pageSize') ?? rawItems.length,
    hasMore: booleanValue(record, 'hasMore') ?? false,
  };
}

export function toCompanyRow(payload: unknown): CompanyRow {
  const record = asRecord(payload);

  return {
    id: stringValue(record, ['id', 'companyId']),
    externalCode: stringValue(record, ['externalCode', 'code']),
    name: stringValue(record, ['name', 'companyName']),
    status: stringValue(record, ['status']),
    createdAtUtc: stringValue(record, ['createdAtUtc']),
    updatedAtUtc: stringValue(record, ['updatedAtUtc']),
  };
}

export function toCompanyAddressRows(payload: unknown): CompanyAddressRow[] {
  return Array.isArray(payload) ? payload.map(toCompanyAddressRow) : [];
}

function toCompanyAddressRow(payload: unknown): CompanyAddressRow {
  const record = asRecord(payload);

  return {
    id: stringValue(record, ['id', 'addressId']),
    addressType: stringValue(record, ['addressType', 'type']),
    countryCode: stringValue(record, ['countryCode']),
    city: stringValue(record, ['city']),
    postalCode: stringValue(record, ['postalCode']),
    line1: stringValue(record, ['line1']),
    line2: stringValue(record, ['line2']),
    createdAtUtc: stringValue(record, ['createdAtUtc']),
    updatedAtUtc: stringValue(record, ['updatedAtUtc']),
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
