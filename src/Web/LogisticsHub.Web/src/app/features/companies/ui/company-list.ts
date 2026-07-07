import { CommonModule } from '@angular/common';
import { Component, EventEmitter, Input, Output } from '@angular/core';
import { EmptyState } from '../../../shared/ui/empty-state/empty-state';
import { CompanyRow } from '../company.models';

@Component({
  selector: 'app-company-list',
  imports: [CommonModule, EmptyState],
  templateUrl: './company-list.html',
  styleUrl: './company-list.css',
})
export class CompanyList {
  @Input({ required: true }) companies: CompanyRow[] = [];
  @Input({ required: true }) selectedCompany: CompanyRow | null = null;
  @Input({ required: true }) hasLoadedCompanies = false;
  @Input({ required: true }) apiLoading = false;
  @Input({ required: true }) apiError = '';

  @Output() companySelected = new EventEmitter<CompanyRow>();

  protected get emptyStateMessage(): string {
    if (!this.hasLoadedCompanies && this.apiLoading) {
      return 'Loading companies...';
    }

    if (!this.hasLoadedCompanies && !this.apiLoading && !this.apiError) {
      return 'Companies will appear here when they are loaded.';
    }

    return 'The response did not contain any company rows.';
  }

  protected isSelectedCompany(company: CompanyRow): boolean {
    return company.id !== undefined && this.selectedCompany?.id === company.id;
  }
}
