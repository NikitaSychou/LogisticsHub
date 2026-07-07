import { CommonModule } from '@angular/common';
import { Component, EventEmitter, Input, Output } from '@angular/core';

export interface InventoryCreateFormModel {
  sku: string;
  name: string;
  quantityAvailable: number;
}

@Component({
  selector: 'app-inventory-create-form',
  imports: [CommonModule],
  templateUrl: './inventory-create-form.html',
  styleUrl: './inventory-create-form.css',
})
export class InventoryCreateForm {
  @Input({ required: true }) form!: InventoryCreateFormModel;
  @Input({ required: true }) creating = false;
  @Input({ required: true }) error = '';

  @Output() submitForm = new EventEmitter<void>();
  @Output() cancel = new EventEmitter<void>();

  protected inputValue(event: Event): string {
    return event.target instanceof HTMLInputElement ? event.target.value : '';
  }

  protected inputNumberValue(event: Event): number {
    const value = this.inputValue(event);
    return value === '' ? 0 : Number(value);
  }
}
