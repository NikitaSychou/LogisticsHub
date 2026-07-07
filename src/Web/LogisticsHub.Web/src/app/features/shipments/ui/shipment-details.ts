import { CommonModule } from '@angular/common';
import { Component, EventEmitter, Input, Output } from '@angular/core';
import { ShipmentRow } from '../shipment.models';

@Component({
  selector: 'app-shipment-details',
  imports: [CommonModule],
  templateUrl: './shipment-details.html',
  styleUrl: './shipment-details.css',
})
export class ShipmentDetails {
  @Input({ required: true }) title = '';
  @Input({ required: true }) shipment!: ShipmentRow | null;
  @Input({ required: true }) refreshing = false;

  @Output() refreshStatus = new EventEmitter<void>();
}
