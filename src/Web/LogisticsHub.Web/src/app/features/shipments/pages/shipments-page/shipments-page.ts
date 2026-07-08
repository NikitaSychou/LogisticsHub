import { CommonModule } from '@angular/common';
import { Component, OnDestroy, inject, signal } from '@angular/core';
import { formatProblemError } from '../../../../core/http/problem-error.mapper';
import { ErrorAlert } from '../../../../shared/ui/error-alert/error-alert';
import { ShipmentApiService } from '../../data-access/shipment-api.service';
import { CreateShipmentRequest, ShipmentRow } from '../../models/shipment.models';
import { ShipmentCreateForm } from '../../ui/shipment-create-form/shipment-create-form';
import { ShipmentDetails } from '../../ui/shipment-details/shipment-details';
import { ShipmentReadById } from '../../ui/shipment-read-by-id/shipment-read-by-id';

@Component({
  selector: 'app-shipments-page',
  imports: [CommonModule, ErrorAlert, ShipmentCreateForm, ShipmentDetails, ShipmentReadById],
  templateUrl: './shipments-page.html',
  styleUrl: './shipments-page.css',
})
export class ShipmentsPage implements OnDestroy {
  private readonly shipmentApi = inject(ShipmentApiService);
  private autoRefreshTimer?: number;

  protected readonly creatingShipment = signal(false);
  protected readonly createShipmentError = signal('');
  protected readonly loadingShipment = signal(false);
  protected readonly loadShipmentError = signal('');
  protected readonly refreshingCreatedShipment = signal(false);
  protected readonly refreshingLoadedShipment = signal(false);
  protected readonly statusRefreshError = signal('');
  protected readonly createdShipment = signal<ShipmentRow | null>(null);
  protected readonly loadedShipment = signal<ShipmentRow | null>(null);
  protected readonly createShipmentResetKey = signal(0);

  protected shipmentIdToLoad = '';

  ngOnDestroy(): void {
    this.stopAutoRefresh();
  }

  protected async submitCreateShipment(request: CreateShipmentRequest): Promise<void> {
    if (this.creatingShipment()) {
      return;
    }

    this.creatingShipment.set(true);
    this.createShipmentError.set('');

    try {
      const shipment = await this.shipmentApi.createShipment(request);
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
      this.loadedShipment.set(await this.shipmentApi.getShipment(shipmentId));
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
      const refreshedShipment = await this.shipmentApi.getShipment(shipment.shipmentId);

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

  private resetCreateShipmentForm(): void {
    this.createShipmentResetKey.update((value) => value + 1);
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

  private formatError(error: unknown, fallback: string): string {
    return formatProblemError(error, fallback);
  }
}
