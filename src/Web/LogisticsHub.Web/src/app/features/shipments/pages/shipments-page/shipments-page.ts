import { Component, OnDestroy, inject, signal } from '@angular/core';
import { formatProblemError } from '../../../../core/http/problem-error.mapper';
import { ShipmentApiService } from '../../data-access/shipment-api.service';
import { CreateShipmentRequest, ShipmentRow } from '../../models/shipment.models';
import { ShipmentActiveDetails } from '../../ui/shipment-active-details/shipment-active-details';
import { ShipmentCreateForm } from '../../ui/shipment-create-form/shipment-create-form';
import { ShipmentReadById } from '../../ui/shipment-read-by-id/shipment-read-by-id';

const RESERVATION_PENDING_STATUS = 'ReservationRequested';
const STATUS_AUTO_REFRESH_INTERVAL_MS = 2500;
const STATUS_AUTO_REFRESH_MAX_ATTEMPTS = 6;

@Component({
  selector: 'app-shipments-page',
  imports: [ShipmentActiveDetails, ShipmentCreateForm, ShipmentReadById],
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
  protected readonly refreshingShipment = signal(false);
  protected readonly statusRefreshError = signal('');
  protected readonly activeShipment = signal<ShipmentRow | null>(null);
  protected readonly activeShipmentTitle = signal('Shipment details');
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
      this.showActiveShipment(shipment, 'Created shipment');
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
      const shipment = await this.shipmentApi.getShipment(shipmentId);
      this.showActiveShipment(shipment, 'Loaded shipment');
      this.statusRefreshError.set('');
      this.startStatusAutoRefresh(shipment);
    } catch (error) {
      this.loadShipmentError.set(this.formatError(error, 'Shipment load failed.'));
      this.stopAutoRefresh();
    } finally {
      this.loadingShipment.set(false);
    }
  }

  protected async refreshActiveShipmentStatus(): Promise<void> {
    await this.refreshShipmentStatus();
  }

  private async refreshShipmentStatus(): Promise<void> {
    const shipment = this.activeShipment();
    if (!shipment?.shipmentId) {
      this.statusRefreshError.set('Shipment ID is required before refreshing status.');
      return;
    }

    if (this.refreshingShipment()) {
      return;
    }

    const shipmentId = shipment.shipmentId;
    this.refreshingShipment.set(true);
    this.statusRefreshError.set('');

    try {
      const refreshedShipment = await this.shipmentApi.getShipment(shipmentId);
      if (this.activeShipment()?.shipmentId !== shipmentId) {
        return;
      }

      this.activeShipment.set(refreshedShipment);
      this.restartStatusAutoRefreshIfNeeded(refreshedShipment);
    } catch (error) {
      if (this.activeShipment()?.shipmentId === shipmentId) {
        this.statusRefreshError.set(this.formatError(error, 'Shipment status refresh failed.'));
        this.stopAutoRefresh();
      }
    } finally {
      this.refreshingShipment.set(false);
    }
  }

  private resetCreateShipmentForm(): void {
    this.createShipmentResetKey.update((value) => value + 1);
  }

  private showActiveShipment(shipment: ShipmentRow, title: string): void {
    this.activeShipment.set(shipment);
    this.activeShipmentTitle.set(title);
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
      this.autoRefreshTimer = undefined;
      void this.refreshShipmentForAutoRefresh();
    }, STATUS_AUTO_REFRESH_INTERVAL_MS);
  }

  private async refreshShipmentForAutoRefresh(): Promise<void> {
    if (this.autoRefreshInFlight || this.refreshingShipment()) {
      this.rescheduleAutoRefreshAfterSkippedAttempt();
      return;
    }

    const shipment = this.activeShipment();
    if (!this.shouldAutoRefresh(shipment)) {
      this.stopAutoRefresh();
      return;
    }

    this.autoRefreshAttempts += 1;
    this.autoRefreshInFlight = true;

    try {
      await this.refreshShipmentStatus();
    } finally {
      this.autoRefreshInFlight = false;
    }
  }

  private rescheduleAutoRefreshAfterSkippedAttempt(): void {
    const shipment = this.activeShipment();
    if (!this.shouldAutoRefresh(shipment)) {
      this.stopAutoRefresh();
      return;
    }

    this.scheduleNextStatusAutoRefresh(shipment);
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
