import { CommonModule } from '@angular/common';
import { Component, EventEmitter, Input, Output } from '@angular/core';

export interface StockAdjustmentFormModel {
  quantity: number;
}

@Component({
  selector: 'app-stock-adjustment-form',
  imports: [CommonModule],
  templateUrl: './stock-adjustment-form.html',
  styleUrl: './stock-adjustment-form.css',
})
export class StockAdjustmentForm {
  @Input({ required: true }) form!: StockAdjustmentFormModel;
  @Input({ required: true }) adjusting = false;
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
