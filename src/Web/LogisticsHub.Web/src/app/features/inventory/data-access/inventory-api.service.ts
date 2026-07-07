import { Injectable } from '@angular/core';
import { ApiHttpClient } from '../../../core/http/api-http-client';
import { CreateInventoryItemRequest, CreateStockAdjustmentRequest } from '../models/inventory.models';

@Injectable({ providedIn: 'root' })
export class InventoryApiService {
  constructor(private readonly api: ApiHttpClient) {}

  async getInventoryItemsPage(pageNumber: number): Promise<string> {
    return this.api.get(`/inventory/inventory-items/page?pageNumber=${pageNumber}`, 'Inventory load');
  }

  async createInventoryItem(request: CreateInventoryItemRequest): Promise<string> {
    return this.api.post('/inventory/inventory-items', request, 'Create inventory item');
  }

  async createStockAdjustment(sku: string, request: CreateStockAdjustmentRequest): Promise<string> {
    return this.api.post(
      `/inventory/inventory-items/${encodeURIComponent(sku)}/stock-adjustments`,
      request,
      'Stock adjustment'
    );
  }
}
