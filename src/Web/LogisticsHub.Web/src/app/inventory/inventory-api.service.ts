import { Injectable } from '@angular/core';
import { environment } from '../../environments/environment';

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

  private async readResponse(response: Response, label: string): Promise<string> {
    const body = await response.text();

    if (!response.ok) {
      throw new Error(`${label} returned ${response.status}: ${body || response.statusText}`);
    }

    return body;
  }
}
