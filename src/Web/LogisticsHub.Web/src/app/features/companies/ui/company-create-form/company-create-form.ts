import { CommonModule } from '@angular/common';
import { Component, EventEmitter, Input, Output } from '@angular/core';

export interface CompanyCreateFormModel {
  name: string;
  externalCode: string;
  status: string;
}

@Component({
  selector: 'app-company-create-form',
  imports: [CommonModule],
  templateUrl: './company-create-form.html',
  styleUrl: './company-create-form.css',
})
export class CompanyCreateForm {
  @Input({ required: true }) form!: CompanyCreateFormModel;
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
