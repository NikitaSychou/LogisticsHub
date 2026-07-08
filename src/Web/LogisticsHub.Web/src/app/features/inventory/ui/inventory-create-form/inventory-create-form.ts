import { CommonModule } from '@angular/common';
import { Component, EventEmitter, Input, OnChanges, Output, SimpleChanges } from '@angular/core';
import { CreateInventoryItemRequest } from '../../models/inventory.models';

@Component({
  selector: 'app-inventory-create-form',
  imports: [CommonModule],
  templateUrl: './inventory-create-form.html',
  styleUrl: './inventory-create-form.css',
})
export class InventoryCreateForm implements OnChanges {
  @Input({ required: true }) creating = false;
  @Input({ required: true }) error = '';
  @Input({ required: true }) resetKey = 0;

  @Output() submitForm = new EventEmitter<CreateInventoryItemRequest>();
  @Output() cancel = new EventEmitter<void>();

  protected readonly form = {
    sku: '',
    name: '',
    quantityAvailable: 0,
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
      sku: this.form.sku.trim(),
      name: this.form.name.trim(),
      quantityAvailable: this.form.quantityAvailable,
    });
  }

  protected isValid(): boolean {
    return (
      this.form.sku.trim().length > 0 &&
      this.form.name.trim().length > 0 &&
      Number.isFinite(this.form.quantityAvailable) &&
      this.form.quantityAvailable >= 0
    );
  }

  protected validationMessage(): string {
    if (!this.hasInteracted) {
      return '';
    }

    if (!this.form.sku.trim() || !this.form.name.trim()) {
      return 'SKU and name are required.';
    }

    if (!Number.isFinite(this.form.quantityAvailable) || this.form.quantityAvailable < 0) {
      return 'Quantity available must be greater than or equal to 0.';
    }

    return '';
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
    this.form.sku = '';
    this.form.name = '';
    this.form.quantityAvailable = 0;
    this.hasInteracted = false;
  }
}
