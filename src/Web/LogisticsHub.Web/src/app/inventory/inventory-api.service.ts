import { Injectable } from '@angular/core';
import { environment } from '../../environments/environment';
import { CreateInventoryItemRequest, CreateStockAdjustmentRequest } from './inventory.models';

@Injectable({ providedIn: 'root' })
export class InventoryApiService {
  async getInventoryItemsPage(pageNumber: number, accessToken: string): Promise<string> {
    const response = await fetch(`${environment.api.gatewayBaseUrl}/inventory/inventory-items/page?pageNumber=${pageNumber}`, {
      headers: {
        Authorization: `Bearer ${accessToken}`,
      },
    });

    return this.readResponse(response, 'Inventory load');
  }

  async createInventoryItem(request: CreateInventoryItemRequest, accessToken: string): Promise<string> {
    const response = await fetch(`${environment.api.gatewayBaseUrl}/inventory/inventory-items`, {
      method: 'POST',
      headers: {
        Authorization: `Bearer ${accessToken}`,
        'Content-Type': 'application/json',
      },
      body: JSON.stringify(request),
    });

    return this.readResponse(response, 'Create inventory item');
  }

  async createStockAdjustment(
    sku: string,
    request: CreateStockAdjustmentRequest,
    accessToken: string
  ): Promise<string> {
    const response = await fetch(
      `${environment.api.gatewayBaseUrl}/inventory/inventory-items/${encodeURIComponent(sku)}/stock-adjustments`,
      {
        method: 'POST',
        headers: {
          Authorization: `Bearer ${accessToken}`,
          'Content-Type': 'application/json',
        },
        body: JSON.stringify(request),
      }
    );

    return this.readResponse(response, 'Stock adjustment');
  }

  private async readResponse(response: Response, label: string): Promise<string> {
    const body = await response.text();

    if (!response.ok) {
      throw new Error(`${label} returned ${response.status}: ${body || response.statusText}`);
    }

    return body;
  }
}
