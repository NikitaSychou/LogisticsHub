import { CommonModule } from '@angular/common';
import { Component, EventEmitter, Input, OnChanges, Output, SimpleChanges } from '@angular/core';
import { CreateShipmentRequest, ShipmentItemFormRow } from '../../models/shipment.models';

@Component({
  selector: 'app-shipment-create-form',
  imports: [CommonModule],
  templateUrl: './shipment-create-form.html',
  styleUrl: './shipment-create-form.css',
})
export class ShipmentCreateForm implements OnChanges {
  @Input({ required: true }) creating = false;
  @Input({ required: true }) error = '';
  @Input({ required: true }) resetKey = 0;

  @Output() submitForm = new EventEmitter<CreateShipmentRequest>();

  protected readonly form = {
    senderCompanyId: '',
    senderAddressId: '',
    receiverCompanyId: '',
    receiverAddressId: '',
    items: [{ sku: '', quantity: 1 }] as ShipmentItemFormRow[],
  };
  protected hasInteracted = false;

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['resetKey'] && !changes['resetKey'].firstChange) {
      this.resetForm();
    }
  }

  protected addItem(): void {
    this.hasInteracted = true;
    this.form.items.push({ sku: '', quantity: 1 });
  }

  protected removeItem(index: number): void {
    this.hasInteracted = true;

    if (this.form.items.length === 1) {
      this.form.items[0] = { sku: '', quantity: 1 };
      return;
    }

    this.form.items.splice(index, 1);
  }

  protected submit(): void {
    this.hasInteracted = true;

    if (!this.isValid()) {
      return;
    }

    this.submitForm.emit({
      items: this.form.items.map((item) => ({
        sku: item.sku.trim(),
        quantity: item.quantity,
      })),
      senderCompanyId: this.form.senderCompanyId.trim(),
      senderAddressId: this.form.senderAddressId.trim(),
      receiverCompanyId: this.form.receiverCompanyId.trim(),
      receiverAddressId: this.form.receiverAddressId.trim(),
    });
  }

  protected isValid(): boolean {
    return (
      this.form.senderCompanyId.trim().length > 0 &&
      this.form.senderAddressId.trim().length > 0 &&
      this.form.receiverCompanyId.trim().length > 0 &&
      this.form.receiverAddressId.trim().length > 0 &&
      this.form.items.length > 0 &&
      this.form.items.every((item) => item.sku.trim().length > 0 && Number.isFinite(item.quantity) && item.quantity > 0)
    );
  }

  protected validationMessage(): string {
    if (!this.hasInteracted) {
      return '';
    }

    const missingReferences = [
      ['sender company ID', this.form.senderCompanyId],
      ['sender address ID', this.form.senderAddressId],
      ['receiver company ID', this.form.receiverCompanyId],
      ['receiver address ID', this.form.receiverAddressId],
    ].filter(([, value]) => !value.trim());

    if (missingReferences.length > 0) {
      return `Missing required fields: ${missingReferences.map(([name]) => name).join(', ')}.`;
    }

    if (this.form.items.length === 0) {
      return 'At least one item is required.';
    }

    for (const [index, item] of this.form.items.entries()) {
      if (!item.sku.trim()) {
        return `Item ${index + 1}: SKU is required.`;
      }

      if (!Number.isFinite(item.quantity) || item.quantity <= 0) {
        return `Item ${index + 1}: quantity must be greater than 0.`;
      }
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
    this.form.senderCompanyId = '';
    this.form.senderAddressId = '';
    this.form.receiverCompanyId = '';
    this.form.receiverAddressId = '';
    this.form.items = [{ sku: '', quantity: 1 }];
    this.hasInteracted = false;
  }
}
