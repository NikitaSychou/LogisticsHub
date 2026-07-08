import { CommonModule } from '@angular/common';
import { Component, OnDestroy, inject, signal } from '@angular/core';
import { formatProblemError } from '../../../../core/http/problem-error.mapper';
import { ErrorAlert } from '../../../../shared/ui/error-alert/error-alert';
import { ShipmentApiService } from '../../data-access/shipment-api.service';
import { CreateShipmentRequest, ShipmentRow } from '../../models/shipment.models';
import { ShipmentCreateForm } from '../../ui/shipment-create-form/shipment-create-form';
import { ShipmentDetails } from '../../ui/shipment-details/shipment-details';
import { ShipmentReadById } from '../../ui/shipment-read-by-id/shipment-read-by-id';

const RESERVATION_PENDING_STATUS = 'ReservationRequested';
const STATUS_AUTO_REFRESH_INTERVAL_MS = 2500;
const STATUS_AUTO_REFRESH_MAX_ATTEMPTS = 6;

type ShipmentRefreshTarget = 'created' | 'loaded';

@Component({
  selector: 'app-shipments-page',
  imports: [CommonModule, ErrorAlert, ShipmentCreateForm, ShipmentDetails, ShipmentReadById],
  templateUrl: './shipments-page.html',
  styleUrl: './shipments-page.css',
})
export class ShipmentsPage implements OnDestroy {
  private readonly shipmentApi = inject(ShipmentApiService);
  private autoRefreshTimer?: number;
  private autoRefreshAttempts = 0;
  private autoRefreshInFlight = false;

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
      this.startStatusAutoRefresh(shipment);
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

  private async refreshShipmentStatus(target: ShipmentRefreshTarget): Promise<void> {
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
        this.restartStatusAutoRefreshIfNeeded(refreshedShipment);
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

  private startStatusAutoRefresh(shipment: ShipmentRow): void {
    this.stopAutoRefresh();
    this.autoRefreshAttempts = 0;
    this.scheduleNextStatusAutoRefresh(shipment);
  }

  private restartStatusAutoRefreshIfNeeded(shipment: ShipmentRow): void {
    if (!this.shouldAutoRefresh(shipment)) {
      this.stopAutoRefresh();
      return;
    }

    this.scheduleNextStatusAutoRefresh(shipment);
  }

  private scheduleNextStatusAutoRefresh(shipment: ShipmentRow): void {
    if (!this.shouldAutoRefresh(shipment) || this.autoRefreshAttempts >= STATUS_AUTO_REFRESH_MAX_ATTEMPTS) {
      this.stopAutoRefresh();
      return;
    }

    this.clearAutoRefreshTimer();
    this.autoRefreshTimer = window.setTimeout(() => {
      void this.refreshCreatedShipmentForAutoRefresh();
    }, STATUS_AUTO_REFRESH_INTERVAL_MS);
  }

  private async refreshCreatedShipmentForAutoRefresh(): Promise<void> {
    if (this.autoRefreshInFlight || this.refreshingCreatedShipment()) {
      return;
    }

    const shipment = this.createdShipment();
    if (!this.shouldAutoRefresh(shipment)) {
      this.stopAutoRefresh();
      return;
    }

    this.autoRefreshAttempts += 1;
    this.autoRefreshInFlight = true;

    try {
      await this.refreshCreatedShipmentStatus();
    } finally {
      this.autoRefreshInFlight = false;
    }
  }

  private shouldAutoRefresh(shipment: ShipmentRow | null): shipment is ShipmentRow {
    return shipment?.status === RESERVATION_PENDING_STATUS && !!shipment.shipmentId;
  }

  private stopAutoRefresh(): void {
    this.clearAutoRefreshTimer();
    this.autoRefreshAttempts = 0;
    this.autoRefreshInFlight = false;
  }

  private clearAutoRefreshTimer(): void {
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
