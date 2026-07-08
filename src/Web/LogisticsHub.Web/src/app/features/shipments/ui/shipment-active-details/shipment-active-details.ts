import { Component, EventEmitter, Input, Output } from '@angular/core';
import { ErrorAlert } from '../../../../shared/ui/error-alert/error-alert';
import { EmptyState } from '../../../../shared/ui/empty-state/empty-state';
import { ShipmentRow } from '../../models/shipment.models';
import { ShipmentDetails } from '../shipment-details/shipment-details';

@Component({
  selector: 'app-shipment-active-details',
  imports: [EmptyState, ErrorAlert, ShipmentDetails],
  templateUrl: './shipment-active-details.html',
})
export class ShipmentActiveDetails {
  @Input({ required: true }) title = '';
  @Input({ required: true }) shipment!: ShipmentRow | null;
  @Input({ required: true }) refreshing = false;
  @Input({ required: true }) statusRefreshError = '';

  @Output() refreshStatus = new EventEmitter<void>();
}
