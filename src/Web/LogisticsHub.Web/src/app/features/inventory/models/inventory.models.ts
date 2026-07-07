import { PagedResponse } from '../../../shared/models/paged-response';

export interface InventoryItemRow {
  sku?: string;
  name?: string;
  quantityAvailable?: number;
  raw: unknown;
}

export interface InventoryItemsPageResult extends PagedResponse<InventoryItemRow> {}

export interface CreateInventoryItemRequest {
  sku: string;
  name: string;
  quantityAvailable: number;
}

export interface CreateStockAdjustmentRequest {
  quantity: number;
}
