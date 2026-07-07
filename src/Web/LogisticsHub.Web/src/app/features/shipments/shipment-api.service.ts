import { Injectable } from '@angular/core';
import { environment } from '../../../environments/environment';
import { CreateShipmentRequest } from './shipment.models';

@Injectable({ providedIn: 'root' })
export class ShipmentApiService {
  async createShipment(request: CreateShipmentRequest, accessToken: string): Promise<string> {
    const response = await fetch(`${environment.api.gatewayBaseUrl}/shipment/shipments`, {
      method: 'POST',
      headers: {
        Authorization: `Bearer ${accessToken}`,
        'Content-Type': 'application/json',
      },
      body: JSON.stringify(request),
    });

    return this.readResponse(response, 'Create shipment');
  }

  async getShipment(shipmentId: string, accessToken: string): Promise<string> {
    const response = await fetch(
      `${environment.api.gatewayBaseUrl}/shipment/shipments/${encodeURIComponent(shipmentId)}`,
      {
        headers: {
          Authorization: `Bearer ${accessToken}`,
        },
      }
    );

    return this.readResponse(response, 'Shipment load');
  }

  private async readResponse(response: Response, label: string): Promise<string> {
    const body = await response.text();

    if (!response.ok) {
      throw new Error(`${label} returned ${response.status}: ${body || response.statusText}`);
    }

    return body;
  }
}
