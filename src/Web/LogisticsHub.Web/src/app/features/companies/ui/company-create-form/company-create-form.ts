import { CommonModule } from '@angular/common';
import { Component, EventEmitter, Input, OnChanges, Output, SimpleChanges } from '@angular/core';
import { CreateCompanyRequest } from '../../models/company.models';

@Component({
  selector: 'app-company-create-form',
  imports: [CommonModule],
  templateUrl: './company-create-form.html',
  styleUrl: './company-create-form.css',
})
export class CompanyCreateForm implements OnChanges {
  @Input({ required: true }) creating = false;
  @Input({ required: true }) error = '';
  @Input({ required: true }) resetKey = 0;

  @Output() submitForm = new EventEmitter<CreateCompanyRequest>();
  @Output() cancel = new EventEmitter<void>();

  protected readonly form = {
    name: '',
    externalCode: '',
    status: 'Active',
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
      name: this.form.name.trim(),
      externalCode: this.optionalTrimmed(this.form.externalCode),
      status: this.form.status.trim(),
    });
  }

  protected isValid(): boolean {
    return this.form.name.trim().length > 0 && this.form.status.trim().length > 0;
  }

  protected validationMessage(): string {
    return this.hasInteracted && !this.isValid() ? 'Name and status are required.' : '';
  }

  protected inputValue(event: Event): string {
    this.hasInteracted = true;

    return event.target instanceof HTMLInputElement || event.target instanceof HTMLSelectElement
      ? event.target.value
      : '';
  }

  private resetForm(): void {
    this.form.name = '';
    this.form.externalCode = '';
    this.form.status = 'Active';
    this.hasInteracted = false;
  }

  private optionalTrimmed(value: string): string | null {
    const trimmed = value.trim();
    return trimmed.length > 0 ? trimmed : null;
  }
}
