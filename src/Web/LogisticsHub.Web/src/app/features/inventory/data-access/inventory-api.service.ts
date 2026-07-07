import { Injectable } from '@angular/core';
import { ApiHttpClient } from '../../../core/http/api-http-client';
import {
  CreateInventoryItemRequest,
  CreateStockAdjustmentRequest,
  InventoryItemRow,
  InventoryItemsPageResult,
} from '../models/inventory.models';
import { toInventoryItemRow, toInventoryItemsPageResult } from './inventory-api.mapper';

@Injectable({ providedIn: 'root' })
export class InventoryApiService {
  constructor(private readonly api: ApiHttpClient) {}

  async getInventoryItemsPage(pageNumber: number): Promise<InventoryItemsPageResult> {
    const payload = await this.api.getJson<unknown>(
      `/inventory/inventory-items/page?pageNumber=${pageNumber}`,
      'Inventory load'
    );
    return toInventoryItemsPageResult(payload, pageNumber);
  }

  async createInventoryItem(request: CreateInventoryItemRequest): Promise<InventoryItemRow> {
    const payload = await this.api.postJson<unknown>('/inventory/inventory-items', request, 'Create inventory item');
    return toInventoryItemRow(payload);
  }

  async createStockAdjustment(sku: string, request: CreateStockAdjustmentRequest): Promise<InventoryItemRow> {
    const payload = await this.api.postJson<unknown>(
      `/inventory/inventory-items/${encodeURIComponent(sku)}/stock-adjustments`,
      request,
      'Stock adjustment'
    );
    return toInventoryItemRow(payload);
  }
}
