export interface InventoryItemRow {
  sku?: string;
  name?: string;
  quantityAvailable?: number;
  raw: unknown;
}

export interface CreateInventoryItemRequest {
  sku: string;
  name: string;
  quantityAvailable: number;
}

export interface CreateStockAdjustmentRequest {
  quantity: number;
}
