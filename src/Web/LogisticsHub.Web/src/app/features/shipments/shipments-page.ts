import { CommonModule } from '@angular/common';
import { Component, Input, OnDestroy, inject, signal } from '@angular/core';
import { AccountInfo } from '@azure/msal-browser';
import { formatProblemError } from '../../core/http/problem-error.mapper';
import { ErrorAlert } from '../../shared/ui/error-alert/error-alert';
import { ShipmentApiService } from './shipment-api.service';
import { CreateShipmentRequest, ShipmentItemFormRow, ShipmentRow } from './shipment.models';
import { ShipmentCreateForm } from './ui/shipment-create-form';
import { ShipmentDetails } from './ui/shipment-details';
import { ShipmentReadById } from './ui/shipment-read-by-id';

@Component({
  selector: 'app-shipments-page',
  imports: [CommonModule, ErrorAlert, ShipmentCreateForm, ShipmentDetails, ShipmentReadById],
  templateUrl: './shipments-page.html',
  styleUrl: './shipments-page.css',
})
export class ShipmentsPage implements OnDestroy {
  private readonly shipmentApi = inject(ShipmentApiService);
  private autoRefreshTimer?: number;

  @Input({ required: true }) accessTokenFactory!: () => Promise<string>;
  @Input({ required: true }) account!: AccountInfo | null;

  protected readonly creatingShipment = signal(false);
  protected readonly createShipmentError = signal('');
  protected readonly loadingShipment = signal(false);
  protected readonly loadShipmentError = signal('');
  protected readonly refreshingCreatedShipment = signal(false);
  protected readonly refreshingLoadedShipment = signal(false);
  protected readonly statusRefreshError = signal('');
  protected readonly createdShipment = signal<ShipmentRow | null>(null);
  protected readonly loadedShipment = signal<ShipmentRow | null>(null);

  protected readonly createShipmentForm = {
    senderCompanyId: '',
    senderAddressId: '',
    receiverCompanyId: '',
    receiverAddressId: '',
    items: [{ sku: '', quantity: 1 }] as ShipmentItemFormRow[],
  };

  protected shipmentIdToLoad = '';

  ngOnDestroy(): void {
    this.stopAutoRefresh();
  }

  protected addItemRow(): void {
    this.createShipmentForm.items.push({ sku: '', quantity: 1 });
  }

  protected removeItemRow(index: number): void {
    if (this.createShipmentForm.items.length === 1) {
      this.createShipmentForm.items[0] = { sku: '', quantity: 1 };
      return;
    }

    this.createShipmentForm.items.splice(index, 1);
  }

  protected async submitCreateShipment(): Promise<void> {
    if (this.creatingShipment()) {
      return;
    }

    const request = this.toCreateShipmentRequest();
    if (!request) {
      return;
    }

    this.creatingShipment.set(true);
    this.createShipmentError.set('');

    try {
      const body = await this.shipmentApi.createShipment(request, await this.accessTokenFactory());
      const shipment = this.toShipmentRow(this.parseBody(body));
      this.createdShipment.set(shipment);
      this.loadedShipment.set(null);
      this.statusRefreshError.set('');
      this.resetCreateShipmentForm();
      this.startAutoRefreshIfNeeded(shipment);
    } catch (error) {
      this.createShipmentError.set(this.formatError(error, 'Create shipment failed.'));
    } finally {
      this.creatingShipment.set(false);
    }
  }

  protected async loadShipment(): Promise<void> {
    if (this.loadingShipment()) {
      return;
    }

    const shipmentId = this.shipmentIdToLoad.trim();
    if (!shipmentId) {
      this.loadShipmentError.set('Shipment ID is required.');
      return;
    }

    this.loadingShipment.set(true);
    this.loadShipmentError.set('');

    try {
      const body = await this.shipmentApi.getShipment(shipmentId, await this.accessTokenFactory());
      this.loadedShipment.set(this.toShipmentRow(this.parseBody(body)));
      this.statusRefreshError.set('');
    } catch (error) {
      this.loadShipmentError.set(this.formatError(error, 'Shipment load failed.'));
    } finally {
      this.loadingShipment.set(false);
    }
  }

  protected async refreshCreatedShipmentStatus(): Promise<void> {
    await this.refreshShipmentStatus('created');
  }

  protected async refreshLoadedShipmentStatus(): Promise<void> {
    await this.refreshShipmentStatus('loaded');
  }

  private async refreshShipmentStatus(target: 'created' | 'loaded'): Promise<void> {
    const shipment = target === 'created'
      ? this.createdShipment()
      : this.loadedShipment();
    if (!shipment?.shipmentId) {
      this.statusRefreshError.set('Shipment ID is required before refreshing status.');
      return;
    }

    const refreshingSignal = target === 'created'
      ? this.refreshingCreatedShipment
      : this.refreshingLoadedShipment;
    if (refreshingSignal()) {
      return;
    }

    refreshingSignal.set(true);
    this.statusRefreshError.set('');

    try {
      const body = await this.shipmentApi.getShipment(shipment.shipmentId, await this.accessTokenFactory());
      const refreshedShipment = this.toShipmentRow(this.parseBody(body));

      if (target === 'created') {
        this.createdShipment.set(refreshedShipment);
        this.startAutoRefreshIfNeeded(refreshedShipment);
      } else {
        this.loadedShipment.set(refreshedShipment);
      }
    } catch (error) {
      this.statusRefreshError.set(this.formatError(error, 'Shipment status refresh failed.'));
      this.stopAutoRefresh();
    } finally {
      refreshingSignal.set(false);
    }
  }

  private toCreateShipmentRequest(): CreateShipmentRequest | null {
    const missingReferences = [
      ['sender company ID', this.createShipmentForm.senderCompanyId],
      ['sender address ID', this.createShipmentForm.senderAddressId],
      ['receiver company ID', this.createShipmentForm.receiverCompanyId],
      ['receiver address ID', this.createShipmentForm.receiverAddressId],
    ].filter(([, value]) => !value.trim());

    if (missingReferences.length > 0) {
      this.createShipmentError.set(`Missing required fields: ${missingReferences.map(([name]) => name).join(', ')}.`);
      return null;
    }

    if (this.createShipmentForm.items.length === 0) {
      this.createShipmentError.set('At least one item is required.');
      return null;
    }

    const items = [];
    for (const [index, item] of this.createShipmentForm.items.entries()) {
      if (!item.sku.trim()) {
        this.createShipmentError.set(`Item ${index + 1}: SKU is required.`);
        return null;
      }

      if (!Number.isFinite(item.quantity) || item.quantity <= 0) {
        this.createShipmentError.set(`Item ${index + 1}: quantity must be greater than 0.`);
        return null;
      }

      items.push({
        sku: item.sku.trim(),
        quantity: item.quantity,
      });
    }

    return {
      items,
      senderCompanyId: this.createShipmentForm.senderCompanyId.trim(),
      senderAddressId: this.createShipmentForm.senderAddressId.trim(),
      receiverCompanyId: this.createShipmentForm.receiverCompanyId.trim(),
      receiverAddressId: this.createShipmentForm.receiverAddressId.trim(),
    };
  }

  private resetCreateShipmentForm(): void {
    this.createShipmentForm.senderCompanyId = '';
    this.createShipmentForm.senderAddressId = '';
    this.createShipmentForm.receiverCompanyId = '';
    this.createShipmentForm.receiverAddressId = '';
    this.createShipmentForm.items = [{ sku: '', quantity: 1 }];
  }

  private startAutoRefreshIfNeeded(shipment: ShipmentRow): void {
    this.stopAutoRefresh();

    if (shipment.status !== 'ReservationRequested' || !shipment.shipmentId) {
      return;
    }

    this.autoRefreshTimer = window.setTimeout(() => {
      void this.refreshCreatedShipmentStatus();
    }, 2500);
  }

  private stopAutoRefresh(): void {
    if (this.autoRefreshTimer === undefined) {
      return;
    }

    window.clearTimeout(this.autoRefreshTimer);
    this.autoRefreshTimer = undefined;
  }

  private parseBody(body: string): unknown {
    if (!body) {
      return null;
    }

    try {
      return JSON.parse(body);
    } catch {
      return body;
    }
  }

  private toShipmentRow(payload: unknown): ShipmentRow {
    const record = this.asRecord(payload);
    const rawItems = Array.isArray(record['items']) ? record['items'] : [];

    return {
      shipmentId: this.stringValue(record, ['shipmentId', 'id']),
      shipmentNumber: this.stringValue(record, ['shipmentNumber']),
      status: this.stringValue(record, ['status']),
      reservationId: this.stringValue(record, ['reservationId']),
      reservationFailureReason: this.stringValue(record, ['reservationFailureReason']),
      senderCompanyId: this.stringValue(record, ['senderCompanyId']),
      senderAddressId: this.stringValue(record, ['senderAddressId']),
      receiverCompanyId: this.stringValue(record, ['receiverCompanyId']),
      receiverAddressId: this.stringValue(record, ['receiverAddressId']),
      comment: this.stringValue(record, ['comment']),
      createdAt: this.stringValue(record, ['createdAt']),
      updatedAt: this.stringValue(record, ['updatedAt']),
      dispatchedAt: this.stringValue(record, ['dispatchedAt']),
      cancelledAt: this.stringValue(record, ['cancelledAt']),
      items: rawItems.map((item) => {
        const itemRecord = this.asRecord(item);
        return {
          sku: this.stringValue(itemRecord, ['sku']),
          quantity: this.numberValue(itemRecord, 'quantity'),
        };
      }),
      raw: payload,
    };
  }

  private formatError(error: unknown, fallback: string): string {
    return formatProblemError(error, fallback);
  }

  private asRecord(value: unknown): Record<string, unknown> {
    return value !== null && typeof value === 'object' ? (value as Record<string, unknown>) : {};
  }

  private stringValue(record: Record<string, unknown>, keys: string[]): string | undefined {
    for (const key of keys) {
      const value = record[key];
      if (typeof value === 'string' && value.trim().length > 0) {
        return value;
      }
    }

    return undefined;
  }

  private numberValue(record: Record<string, unknown>, key: string): number | undefined {
    const value = record[key];
    return typeof value === 'number' && Number.isFinite(value) ? value : undefined;
  }
}
