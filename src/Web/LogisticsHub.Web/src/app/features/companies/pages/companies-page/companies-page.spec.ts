import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ApiAuthContext } from '../../../../core/http/api-auth-context';
import { CompanyApiService } from '../../data-access/company-api.service';
import { CompanyPageResult, CompanyRow, CreateCompanyRequest } from '../../models/company.models';
import { CompaniesPage } from './companies-page';

describe('CompaniesPage company creation', () => {
  let fixture: ComponentFixture<CompaniesPage>;
  let api: FakeCompanyApiService;

  beforeEach(async () => {
    api = new FakeCompanyApiService();

    await TestBed.configureTestingModule({
      imports: [CompaniesPage],
      providers: [
        { provide: CompanyApiService, useValue: api },
        { provide: ApiAuthContext, useValue: { account: () => ({ username: 'review-user' }) } },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(CompaniesPage);
    fixture.detectChanges();
  });

  it('calls the create company API and refreshes the company list after successful submission', async () => {
    const request: CreateCompanyRequest = {
      name: 'Acme Logistics',
      externalCode: 'ACM-001',
      status: 'Active',
    };

    api.resetCalls();

    await submitCreateCompany(fixture, request);

    expect(api.createCompanyRequests).toEqual([request]);
    expect(api.requestedPages).toEqual([1]);
  });
});

function submitCreateCompany(fixture: ComponentFixture<CompaniesPage>, request: CreateCompanyRequest): Promise<void> {
  return (fixture.componentInstance as unknown as CompaniesPageTestApi).submitCreateCompany(request);
}

interface CompaniesPageTestApi {
  submitCreateCompany(request: CreateCompanyRequest): Promise<void>;
}

class FakeCompanyApiService {
  readonly createCompanyRequests: CreateCompanyRequest[] = [];
  readonly requestedPages: number[] = [];

  resetCalls(): void {
    this.createCompanyRequests.length = 0;
    this.requestedPages.length = 0;
  }

  async getCompaniesPage(pageNumber: number): Promise<CompanyPageResult> {
    this.requestedPages.push(pageNumber);

    return {
      items: [createdCompany],
      pageNumber,
      pageSize: 20,
      hasMore: false,
    };
  }

  async createCompany(request: CreateCompanyRequest): Promise<CompanyRow> {
    this.createCompanyRequests.push(request);
    return createdCompany;
  }

  async getCompanyAddresses(): Promise<[]> {
    return [];
  }
}

const createdCompany: CompanyRow = {
  id: 'company-1',
  externalCode: 'ACM-001',
  name: 'Acme Logistics',
  status: 'Active',
};