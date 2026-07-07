import { CommonModule } from '@angular/common';
import { Component, EventEmitter, Input, Output } from '@angular/core';

export interface AddressCreateFormModel {
  addressType: string;
  countryCode: string;
  city: string;
  postalCode: string;
  line1: string;
  line2: string;
}

@Component({
  selector: 'app-address-create-form',
  imports: [CommonModule],
  templateUrl: './address-create-form.html',
  styleUrl: './address-create-form.css',
})
export class AddressCreateForm {
  @Input({ required: true }) form!: AddressCreateFormModel;
  @Input({ required: true }) creating = false;
  @Input({ required: true }) error = '';

  @Output() submitForm = new EventEmitter<void>();
  @Output() cancel = new EventEmitter<void>();

  protected inputValue(event: Event): string {
    return event.target instanceof HTMLInputElement || event.target instanceof HTMLSelectElement
      ? event.target.value
      : '';
  }
}
