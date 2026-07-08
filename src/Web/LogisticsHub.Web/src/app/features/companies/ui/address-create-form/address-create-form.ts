import { CommonModule } from '@angular/common';
import { Component, EventEmitter, Input, OnChanges, Output, SimpleChanges } from '@angular/core';
import { CreateCompanyAddressRequest } from '../../models/company.models';

@Component({
  selector: 'app-address-create-form',
  imports: [CommonModule],
  templateUrl: './address-create-form.html',
  styleUrl: './address-create-form.css',
})
export class AddressCreateForm implements OnChanges {
  @Input({ required: true }) creating = false;
  @Input({ required: true }) error = '';
  @Input({ required: true }) resetKey = 0;

  @Output() submitForm = new EventEmitter<CreateCompanyAddressRequest>();
  @Output() cancel = new EventEmitter<void>();

  protected readonly form = {
    addressType: 'Shipping',
    countryCode: '',
    city: '',
    postalCode: '',
    line1: '',
    line2: '',
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
      addressType: this.form.addressType.trim(),
      countryCode: this.form.countryCode.trim().toUpperCase(),
      city: this.form.city.trim(),
      postalCode: this.optionalTrimmed(this.form.postalCode),
      line1: this.form.line1.trim(),
      line2: this.optionalTrimmed(this.form.line2),
    });
  }

  protected isValid(): boolean {
    return (
      this.form.addressType.trim().length > 0 &&
      this.form.countryCode.trim().length > 0 &&
      this.form.city.trim().length > 0 &&
      this.form.line1.trim().length > 0
    );
  }

  protected validationMessage(): string {
    return this.hasInteracted && !this.isValid()
      ? 'Address type, country code, city, and line 1 are required.'
      : '';
  }

  protected inputValue(event: Event): string {
    this.hasInteracted = true;

    return event.target instanceof HTMLInputElement || event.target instanceof HTMLSelectElement
      ? event.target.value
      : '';
  }

  private resetForm(): void {
    this.form.addressType = 'Shipping';
    this.form.countryCode = '';
    this.form.city = '';
    this.form.postalCode = '';
    this.form.line1 = '';
    this.form.line2 = '';
    this.hasInteracted = false;
  }

  private optionalTrimmed(value: string): string | null {
    const trimmed = value.trim();
    return trimmed.length > 0 ? trimmed : null;
  }
}
