import { Injectable } from '@angular/core';
import { gatewayBaseUrl } from '../../core/http/api-config';
import { CreateCompanyAddressRequest, CreateCompanyRequest } from './company.models';

@Injectable({ providedIn: 'root' })
export class CompanyApiService {
  async getCompaniesPage(pageNumber: number, accessToken: string): Promise<string> {
    const response = await fetch(`${gatewayBaseUrl}/company/companies/page?pageNumber=${pageNumber}`, {
      headers: {
        Authorization: `Bearer ${accessToken}`,
      },
    });

    return this.readResponse(response, 'Gateway');
  }

  async getCompanyAddresses(companyId: string, accessToken: string): Promise<string> {
    const response = await fetch(
      `${gatewayBaseUrl}/company/companies/${companyId}/addresses`,
      {
        headers: {
          Authorization: `Bearer ${accessToken}`,
        },
      }
    );

    return this.readResponse(response, 'Address load');
  }

  async createCompany(request: CreateCompanyRequest, accessToken: string): Promise<string> {
    const response = await fetch(`${gatewayBaseUrl}/company/companies`, {
      method: 'POST',
      headers: {
        Authorization: `Bearer ${accessToken}`,
        'Content-Type': 'application/json',
      },
      body: JSON.stringify(request),
    });

    return this.readResponse(response, 'Create company');
  }

  async createCompanyAddress(
    companyId: string,
    request: CreateCompanyAddressRequest,
    accessToken: string
  ): Promise<string> {
    const response = await fetch(
      `${gatewayBaseUrl}/company/companies/${companyId}/addresses`,
      {
        method: 'POST',
        headers: {
          Authorization: `Bearer ${accessToken}`,
          'Content-Type': 'application/json',
        },
        body: JSON.stringify(request),
      }
    );

    return this.readResponse(response, 'Create address');
  }

  private async readResponse(response: Response, label: string): Promise<string> {
    const body = await response.text();

    if (!response.ok) {
      throw new Error(`${label} returned ${response.status}: ${body || response.statusText}`);
    }

    return body;
  }
}
