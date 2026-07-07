import { Injectable } from '@angular/core';
import { ApiHttpClient } from '../../../core/http/api-http-client';
import { CreateShipmentRequest } from '../models/shipment.models';

@Injectable({ providedIn: 'root' })
export class ShipmentApiService {
  constructor(private readonly api: ApiHttpClient) {}

  async createShipment(request: CreateShipmentRequest): Promise<string> {
    return this.api.post('/shipment/shipments', request, 'Create shipment');
  }

  async getShipment(shipmentId: string): Promise<string> {
    return this.api.get(`/shipment/shipments/${encodeURIComponent(shipmentId)}`, 'Shipment load');
  }
}
