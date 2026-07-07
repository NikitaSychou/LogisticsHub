export interface ShipmentItemRequest {
  sku: string;
  quantity: number;
}

export interface ShipmentItemFormRow {
  sku: string;
  quantity: number;
}

export interface CreateShipmentRequest {
  items: ShipmentItemRequest[];
  senderCompanyId: string;
  senderAddressId: string;
  receiverCompanyId: string;
  receiverAddressId: string;
}

export interface ShipmentItemRow {
  sku?: string;
  quantity?: number;
}

export interface ShipmentRow {
  shipmentId?: string;
  shipmentNumber?: string;
  status?: string;
  reservationId?: string;
  reservationFailureReason?: string;
  senderCompanyId?: string;
  senderAddressId?: string;
  receiverCompanyId?: string;
  receiverAddressId?: string;
  comment?: string;
  createdAt?: string;
  updatedAt?: string;
  dispatchedAt?: string;
  cancelledAt?: string;
  items: ShipmentItemRow[];
  raw: unknown;
}
