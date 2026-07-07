import { Injectable } from '@angular/core';
import { environment } from '../../environments/environment';

@Injectable({ providedIn: 'root' })
export class CompanyApiService {
  async getCompaniesPage(pageNumber: number, accessToken: string): Promise<string> {
    const response = await fetch(`${environment.api.gatewayBaseUrl}/company/companies/page?pageNumber=${pageNumber}`, {
      headers: {
        Authorization: `Bearer ${accessToken}`,
      },
    });

    return this.readResponse(response, 'Gateway');
  }

  async getCompanyAddresses(companyId: string, accessToken: string): Promise<string> {
    const response = await fetch(
      `${environment.api.gatewayBaseUrl}/company/companies/${companyId}/addresses`,
      {
        headers: {
          Authorization: `Bearer ${accessToken}`,
        },
      }
    );

    return this.readResponse(response, 'Address load');
  }

  private async readResponse(response: Response, label: string): Promise<string> {
    const body = await response.text();

    if (!response.ok) {
      throw new Error(`${label} returned ${response.status}: ${body || response.statusText}`);
    }

    return body;
  }
}
