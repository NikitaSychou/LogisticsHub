import { Injectable } from '@angular/core';
import { ApiHttpClient } from '../../../core/http/api-http-client';
import { CreateShipmentRequest, ShipmentRow } from '../models/shipment.models';
import { toShipmentRow } from './shipment-api.mapper';

@Injectable({ providedIn: 'root' })
export class ShipmentApiService {
  constructor(private readonly api: ApiHttpClient) {}

  async createShipment(request: CreateShipmentRequest): Promise<ShipmentRow> {
    const payload = await this.api.postJson<unknown>('/shipment/shipments', request, 'Create shipment');
    return toShipmentRow(payload);
  }

  async getShipment(shipmentId: string): Promise<ShipmentRow> {
    const payload = await this.api.getJson<unknown>(
      `/shipment/shipments/${encodeURIComponent(shipmentId)}`,
      'Shipment load'
    );
    return toShipmentRow(payload);
  }
}
