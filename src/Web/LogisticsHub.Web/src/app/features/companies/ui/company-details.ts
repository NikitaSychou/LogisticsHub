import { CommonModule } from '@angular/common';
import { Component, EventEmitter, Input, Output } from '@angular/core';
import { CompanyAddressRow, CompanyRow } from '../company.models';
import { AddressCreateForm, AddressCreateFormModel } from './address-create-form';

@Component({
  selector: 'app-company-details',
  imports: [CommonModule, AddressCreateForm],
  templateUrl: './company-details.html',
  styleUrl: './company-details.css',
})
export class CompanyDetails {
  @Input({ required: true }) company!: CompanyRow | null;
  @Input({ required: true }) addresses: CompanyAddressRow[] = [];
  @Input({ required: true }) addressesLoading = false;
  @Input({ required: true }) addressesLoaded = false;
  @Input({ required: true }) addressError = '';
  @Input({ required: true }) showCreateAddressForm = false;
  @Input({ required: true }) creatingAddress = false;
  @Input({ required: true }) createAddressError = '';
  @Input({ required: true }) createAddressForm!: AddressCreateFormModel;

  @Output() toggleCreateAddress = new EventEmitter<void>();
  @Output() submitCreateAddress = new EventEmitter<void>();
  @Output() cancelCreateAddress = new EventEmitter<void>();
}
