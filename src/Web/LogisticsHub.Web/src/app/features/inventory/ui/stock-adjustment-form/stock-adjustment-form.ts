import { CommonModule } from '@angular/common';
import { Component, EventEmitter, Input, OnChanges, Output, SimpleChanges } from '@angular/core';
import { CreateStockAdjustmentRequest } from '../../models/inventory.models';

@Component({
  selector: 'app-stock-adjustment-form',
  imports: [CommonModule],
  templateUrl: './stock-adjustment-form.html',
  styleUrl: './stock-adjustment-form.css',
})
export class StockAdjustmentForm implements OnChanges {
  @Input({ required: true }) adjusting = false;
  @Input({ required: true }) error = '';
  @Input({ required: true }) resetKey = 0;

  @Output() submitForm = new EventEmitter<CreateStockAdjustmentRequest>();
  @Output() cancel = new EventEmitter<void>();

  protected readonly form = {
    quantity: 1,
  };
  protected hasInteracted = false;

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['resetKey'] && !changes['resetKey'].firstChange) {
      this.resetForm();
    }
  }

  protected submit(): void {
    this.hasInteracted = true;

    if (!this.isValid()) {
      return;
    }

    this.submitForm.emit({
      quantity: this.form.quantity,
    });
  }

  protected isValid(): boolean {
    return Number.isFinite(this.form.quantity) && this.form.quantity > 0;
  }

  protected validationMessage(): string {
    return this.hasInteracted && !this.isValid() ? 'Quantity must be greater than 0.' : '';
  }

  protected inputValue(event: Event): string {
    this.hasInteracted = true;

    return event.target instanceof HTMLInputElement ? event.target.value : '';
  }

  protected inputNumberValue(event: Event): number {
    const value = this.inputValue(event);
    return value === '' ? 0 : Number(value);
  }

  private resetForm(): void {
    this.form.quantity = 1;
    this.hasInteracted = false;
  }
}
