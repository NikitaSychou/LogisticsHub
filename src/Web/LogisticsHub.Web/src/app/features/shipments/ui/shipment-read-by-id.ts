import { CommonModule } from '@angular/common';
import { Component, EventEmitter, Input, Output } from '@angular/core';

@Component({
  selector: 'app-shipment-read-by-id',
  imports: [CommonModule],
  templateUrl: './shipment-read-by-id.html',
  styleUrl: './shipment-read-by-id.css',
})
export class ShipmentReadById {
  @Input({ required: true }) shipmentId = '';
  @Input({ required: true }) loading = false;
  @Input({ required: true }) error = '';

  @Output() shipmentIdChange = new EventEmitter<string>();
  @Output() loadShipment = new EventEmitter<void>();

  protected inputValue(event: Event): string {
    return event.target instanceof HTMLInputElement ? event.target.value : '';
  }
}
