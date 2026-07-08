import { toInventoryItemRow, toInventoryItemsPageResult } from './inventory-api.mapper';

describe('inventory API mapper', () => {
  it('maps paged inventory item responses', () => {
    const result = toInventoryItemsPageResult(
      {
        items: [{ sku: 'SKU-1', name: 'Widget', quantityAvailable: 12 }],
        pageNumber: 4,
        pageSize: 25,
        hasMore: true,
      },
      1
    );

    expect(result).toEqual({
      items: [{ sku: 'SKU-1', name: 'Widget', quantityAvailable: 12 }],
      pageNumber: 4,
      pageSize: 25,
      hasMore: true,
    });
  });

  it('falls back to requested page, item count, and no more pages when paging metadata is missing', () => {
    const result = toInventoryItemsPageResult(
      {
        items: [
          { sku: 'SKU-1', name: 'Widget', quantityAvailable: 12 },
          { sku: 'SKU-2', name: 'Cable', quantityAvailable: 0 },
        ],
      },
      2
    );

    expect(result.pageNumber).toBe(2);
    expect(result.pageSize).toBe(2);
    expect(result.hasMore).toBe(false);
  });

  it('preserves zero available quantity as a valid mapped value', () => {
    expect(toInventoryItemRow({ sku: 'SKU-ZERO', name: 'Starter stock', quantityAvailable: 0 })).toEqual({
      sku: 'SKU-ZERO',
      name: 'Starter stock',
      quantityAvailable: 0,
    });
  });

  it('drops invalid string and number fields instead of leaking raw values', () => {
    expect(toInventoryItemRow({ sku: '   ', name: null, quantityAvailable: Number.NaN })).toEqual({
      sku: undefined,
      name: undefined,
      quantityAvailable: undefined,
    });
  });
});
