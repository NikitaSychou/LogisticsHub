import { CommonModule } from '@angular/common';
import { Component, EventEmitter, Input, Output } from '@angular/core';
import { ShipmentItemFormRow } from '../shipment.models';

export interface ShipmentCreateFormModel {
  senderCompanyId: string;
  senderAddressId: string;
  receiverCompanyId: string;
  receiverAddressId: string;
  items: ShipmentItemFormRow[];
}

@Component({
  selector: 'app-shipment-create-form',
  imports: [CommonModule],
  templateUrl: './shipment-create-form.html',
  styleUrl: './shipment-create-form.css',
})
export class ShipmentCreateForm {
  @Input({ required: true }) form!: ShipmentCreateFormModel;
  @Input({ required: true }) creating = false;
  @Input({ required: true }) error = '';

  @Output() addItem = new EventEmitter<void>();
  @Output() removeItem = new EventEmitter<number>();
  @Output() submitForm = new EventEmitter<void>();

  protected inputValue(event: Event): string {
    return event.target instanceof HTMLInputElement ? event.target.value : '';
  }

  protected inputNumberValue(event: Event): number {
    const value = this.inputValue(event);
    return value === '' ? 0 : Number(value);
  }
}
