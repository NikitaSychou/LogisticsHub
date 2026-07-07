import { CommonModule } from '@angular/common';
import { Component, EventEmitter, Input, Output } from '@angular/core';
import { CompanyRow } from '../company.models';

@Component({
  selector: 'app-company-list',
  imports: [CommonModule],
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

  protected isSelectedCompany(company: CompanyRow): boolean {
    return company.id !== undefined && this.selectedCompany?.id === company.id;
  }
}
