import { toShipmentRow } from './shipment-api.mapper';

describe('shipment API mapper', () => {
  it('maps shipment details, id alias, status, reservation fields, and items', () => {
    const result = toShipmentRow({
      id: 'shipment-1',
      shipmentNumber: 'SHP-0001',
      status: 'ReservationRequested',
      reservationId: 'reservation-1',
      reservationFailureReason: 'Waiting for inventory',
      senderCompanyId: 'sender-company',
      senderAddressId: 'sender-address',
      receiverCompanyId: 'receiver-company',
      receiverAddressId: 'receiver-address',
      comment: 'Handle with care',
      createdAt: '2026-01-01T00:00:00Z',
      updatedAt: '2026-01-01T00:01:00Z',
      dispatchedAt: '2026-01-02T00:00:00Z',
      cancelledAt: '2026-01-03T00:00:00Z',
      items: [{ sku: 'SKU-1', quantity: 3 }],
    });

    expect(result).toEqual({
      shipmentId: 'shipment-1',
      shipmentNumber: 'SHP-0001',
      status: 'ReservationRequested',
      reservationId: 'reservation-1',
      reservationFailureReason: 'Waiting for inventory',
      senderCompanyId: 'sender-company',
      senderAddressId: 'sender-address',
      receiverCompanyId: 'receiver-company',
      receiverAddressId: 'receiver-address',
      comment: 'Handle with care',
      createdAt: '2026-01-01T00:00:00Z',
      updatedAt: '2026-01-01T00:01:00Z',
      dispatchedAt: '2026-01-02T00:00:00Z',
      cancelledAt: '2026-01-03T00:00:00Z',
      items: [{ sku: 'SKU-1', quantity: 3 }],
    });
  });

  it('prefers shipmentId when both shipmentId and id are present', () => {
    expect(toShipmentRow({ shipmentId: 'shipment-primary', id: 'shipment-alias', items: [] }).shipmentId).toBe(
      'shipment-primary'
    );
  });

  it('maps missing or non-array items to an empty list', () => {
    expect(toShipmentRow({ shipmentId: 'shipment-1' }).items).toEqual([]);
    expect(toShipmentRow({ shipmentId: 'shipment-1', items: null }).items).toEqual([]);
  });

  it('drops invalid shipment item fields instead of leaking raw values', () => {
    const result = toShipmentRow({
      shipmentId: 'shipment-1',
      items: [{ sku: '   ', quantity: Number.POSITIVE_INFINITY }],
    });

    expect(result.items).toEqual([{ sku: undefined, quantity: undefined }]);
  });
});
