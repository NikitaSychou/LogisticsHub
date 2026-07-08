import { ShipmentRow } from '../models/shipment.models';

export function toShipmentRow(payload: unknown): ShipmentRow {
  const record = asRecord(payload);
  const rawItems = Array.isArray(record['items']) ? record['items'] : [];

  return {
    shipmentId: stringValue(record, ['shipmentId', 'id']),
    shipmentNumber: stringValue(record, ['shipmentNumber']),
    status: stringValue(record, ['status']),
    reservationId: stringValue(record, ['reservationId']),
    reservationFailureReason: stringValue(record, ['reservationFailureReason']),
    senderCompanyId: stringValue(record, ['senderCompanyId']),
    senderAddressId: stringValue(record, ['senderAddressId']),
    receiverCompanyId: stringValue(record, ['receiverCompanyId']),
    receiverAddressId: stringValue(record, ['receiverAddressId']),
    comment: stringValue(record, ['comment']),
    createdAt: stringValue(record, ['createdAt']),
    updatedAt: stringValue(record, ['updatedAt']),
    dispatchedAt: stringValue(record, ['dispatchedAt']),
    cancelledAt: stringValue(record, ['cancelledAt']),
    items: rawItems.map((item) => {
      const itemRecord = asRecord(item);
      return {
        sku: stringValue(itemRecord, ['sku']),
        quantity: numberValue(itemRecord, 'quantity'),
      };
    }),
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
