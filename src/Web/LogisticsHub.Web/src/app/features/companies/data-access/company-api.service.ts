import { Injectable } from '@angular/core';
import { ApiHttpClient } from '../../../core/http/api-http-client';
import { CreateCompanyAddressRequest, CreateCompanyRequest } from '../models/company.models';

@Injectable({ providedIn: 'root' })
export class CompanyApiService {
  constructor(private readonly api: ApiHttpClient) {}

  async getCompaniesPage(pageNumber: number): Promise<string> {
    return this.api.get(`/company/companies/page?pageNumber=${pageNumber}`, 'Gateway');
  }

  async getCompanyAddresses(companyId: string): Promise<string> {
    return this.api.get(`/company/companies/${companyId}/addresses`, 'Address load');
  }

  async createCompany(request: CreateCompanyRequest): Promise<string> {
    return this.api.post('/company/companies', request, 'Create company');
  }

  async createCompanyAddress(companyId: string, request: CreateCompanyAddressRequest): Promise<string> {
    return this.api.post(`/company/companies/${companyId}/addresses`, request, 'Create address');
  }
}
