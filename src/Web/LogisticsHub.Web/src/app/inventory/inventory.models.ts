export interface InventoryItemRow {
  sku?: string;
  name?: string;
  quantityAvailable?: number;
  raw: unknown;
}

export interface PagedResponse<T> {
  items: T[];
  pageNumber: number;
  pageSize: number;
  hasMore: boolean;
}
