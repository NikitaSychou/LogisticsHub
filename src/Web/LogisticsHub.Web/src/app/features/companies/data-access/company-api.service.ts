import { Injectable } from '@angular/core';
import { ApiHttpClient } from '../../../core/http/api-http-client';
import {
  CompanyAddressRow,
  CompanyPageResult,
  CompanyRow,
  CreateCompanyAddressRequest,
  CreateCompanyRequest,
} from '../models/company.models';
import { toCompanyAddressRows, toCompanyPageResult, toCompanyRow } from './company-api.mapper';

@Injectable({ providedIn: 'root' })
export class CompanyApiService {
  constructor(private readonly api: ApiHttpClient) {}

  async getCompaniesPage(pageNumber: number): Promise<CompanyPageResult> {
    const payload = await this.api.getJson<unknown>(`/company/companies/page?pageNumber=${pageNumber}`, 'Gateway');
    return toCompanyPageResult(payload, pageNumber);
  }

  async getCompanyAddresses(companyId: string): Promise<CompanyAddressRow[]> {
    const payload = await this.api.getJson<unknown>(`/company/companies/${companyId}/addresses`, 'Address load');
    return toCompanyAddressRows(payload);
  }

  async createCompany(request: CreateCompanyRequest): Promise<CompanyRow> {
    const payload = await this.api.postJson<unknown>('/company/companies', request, 'Create company');
    return toCompanyRow(payload);
  }

  async createCompanyAddress(companyId: string, request: CreateCompanyAddressRequest): Promise<void> {
    await this.api.postJson<unknown>(`/company/companies/${companyId}/addresses`, request, 'Create address');
  }
}
