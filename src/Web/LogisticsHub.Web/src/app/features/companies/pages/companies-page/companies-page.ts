import { CommonModule } from '@angular/common';
import {
  AfterViewInit,
  Component,
  ElementRef,
  OnDestroy,
  ViewChild,
  computed,
  inject,
  signal,
} from '@angular/core';
import { ApiAuthContext } from '../../../../core/http/api-auth-context';
import { formatProblemError } from '../../../../core/http/problem-error.mapper';
import { ErrorAlert } from '../../../../shared/ui/error-alert/error-alert';
import { LoadMoreState } from '../../../../shared/ui/load-more-state/load-more-state';
import { CompanyApiService } from '../../data-access/company-api.service';
import {
  CompanyAddressRow,
  CompanyRow,
  CreateCompanyAddressRequest,
  CreateCompanyRequest,
} from '../../models/company.models';
import { CompanyCreateForm } from '../../ui/company-create-form/company-create-form';
import { CompanyDetails } from '../../ui/company-details/company-details';
import { CompanyList } from '../../ui/company-list/company-list';

@Component({
  selector: 'app-companies-page',
  imports: [CommonModule, CompanyCreateForm, CompanyDetails, CompanyList, ErrorAlert, LoadMoreState],
  templateUrl: './companies-page.html',
  styleUrl: './companies-page.css',
})
export class CompaniesPage implements AfterViewInit, OnDestroy {
  private readonly companyApi = inject(CompanyApiService);
  private readonly apiAuthContext = inject(ApiAuthContext);
  private observer?: IntersectionObserver;
  private sentinelElement?: HTMLElement;
  private viewReady = false;
  private addressLoadVersion = 0;

  @ViewChild('companiesScrollSentinel')
  private set companiesScrollSentinel(value: ElementRef<HTMLElement> | undefined) {
    this.sentinelElement = value?.nativeElement;
    this.initializeCompaniesObserver();
  }

  protected readonly apiLoading = signal(false);
  protected readonly loadingMore = signal(false);
  protected readonly apiError = signal('');
  protected readonly companies = signal<CompanyRow[]>([]);
  protected readonly selectedCompany = signal<CompanyRow | null>(null);
  protected readonly companyAddresses = signal<CompanyAddressRow[]>([]);
  protected readonly addressesLoading = signal(false);
  protected readonly addressesLoaded = signal(false);
  protected readonly addressError = signal('');
  protected readonly showCreateCompanyForm = signal(false);
  protected readonly creatingCompany = signal(false);
  protected readonly createCompanyError = signal('');
  protected readonly showCreateAddressForm = signal(false);
  protected readonly creatingAddress = signal(false);
  protected readonly createAddressError = signal('');
  protected readonly hasLoadedCompanies = signal(false);
  protected readonly currentCompaniesPage = signal(0);
  protected readonly companiesPageSize = signal(0);
  protected readonly hasMoreCompanies = signal(false);
  protected readonly isCompaniesLoading = computed(() => this.apiLoading() || this.loadingMore());

  protected readonly createCompanyForm = {
    name: '',
    externalCode: '',
    status: 'Active',
  };

  protected readonly createAddressForm = {
    addressType: 'Shipping',
    countryCode: '',
    city: '',
    postalCode: '',
    line1: '',
    line2: '',
  };

  ngAfterViewInit(): void {
    this.viewReady = true;
    this.initializeCompaniesObserver();
    void this.ensureCompaniesLoaded();
  }

  ngOnDestroy(): void {
    this.observer?.disconnect();
  }

  protected async loadCompanies(): Promise<void> {
    await this.loadCompaniesPage(1, { reset: true });
  }

  protected async loadMoreCompanies(): Promise<void> {
    if (!this.hasMoreCompanies()) {
      return;
    }

    await this.loadCompaniesPage(this.currentCompaniesPage() + 1, { reset: false });
  }

  protected async selectCompany(company: CompanyRow): Promise<void> {
    this.selectedCompany.set(company);
    this.companyAddresses.set([]);
    this.addressesLoaded.set(false);
    this.addressError.set('');
    this.showCreateAddressForm.set(false);
    this.createAddressError.set('');
    this.resetCreateAddressForm();

    if (!company.id) {
      this.addressError.set('Cannot load addresses because the selected company has no ID.');
      return;
    }

    await this.loadCompanyAddresses(company);
  }

  protected toggleCreateCompanyForm(): void {
    this.showCreateCompanyForm.update((value) => !value);
    this.createCompanyError.set('');
  }

  protected cancelCreateCompany(): void {
    this.showCreateCompanyForm.set(false);
    this.createCompanyError.set('');
    this.resetCreateCompanyForm();
  }

  protected async submitCreateCompany(): Promise<void> {
    if (this.creatingCompany()) {
      return;
    }

    const request = this.toCreateCompanyRequest();
    if (!request) {
      return;
    }

    this.creatingCompany.set(true);
    this.createCompanyError.set('');

    try {
      const createdCompany = await this.companyApi.createCompany(request);

      this.showCreateCompanyForm.set(false);
      this.resetCreateCompanyForm();
      await this.loadCompaniesPage(1, { reset: true });

      if (createdCompany?.id) {
        const listCompany = this.companies().find((company) => company.id === createdCompany.id) ?? createdCompany;
        await this.selectCompany(listCompany);
      }
    } catch (error) {
      this.createCompanyError.set(this.formatError(error, 'Create company failed.'));
    } finally {
      this.creatingCompany.set(false);
    }
  }

  protected toggleCreateAddressForm(): void {
    this.showCreateAddressForm.update((value) => !value);
    this.createAddressError.set('');
  }

  protected cancelCreateAddress(): void {
    this.showCreateAddressForm.set(false);
    this.createAddressError.set('');
    this.resetCreateAddressForm();
  }

  protected async submitCreateAddress(): Promise<void> {
    if (this.creatingAddress()) {
      return;
    }

    const company = this.selectedCompany();
    if (!company?.id) {
      this.createAddressError.set('Select a company before adding an address.');
      return;
    }

    const request = this.toCreateAddressRequest();
    if (!request) {
      return;
    }

    this.creatingAddress.set(true);
    this.createAddressError.set('');

    try {
      await this.companyApi.createCompanyAddress(company.id, request);
      this.showCreateAddressForm.set(false);
      this.resetCreateAddressForm();
      await this.loadCompanyAddresses(company);
    } catch (error) {
      this.createAddressError.set(this.formatError(error, 'Create address failed.'));
    } finally {
      this.creatingAddress.set(false);
    }
  }

  private async loadCompaniesPage(pageNumber: number, options: { reset: boolean }): Promise<void> {
    if (this.isCompaniesLoading()) {
      return;
    }

    if (!this.apiAuthContext.account()) {
      this.apiError.set('Sign in before calling the Gateway.');
      return;
    }

    this.apiLoading.set(options.reset);
    this.loadingMore.set(!options.reset);
    this.apiError.set('');

    if (options.reset) {
      this.resetCompaniesPageState();
    }

    try {
      const page = await this.companyApi.getCompaniesPage(pageNumber);
      this.applyCompaniesPage(page.items, {
        pageNumber: page.pageNumber,
        pageSize: page.pageSize,
        hasMore: page.hasMore,
        reset: options.reset,
      });
      this.hasLoadedCompanies.set(true);
    } catch (error) {
      this.apiError.set(error instanceof Error ? error.message : 'Gateway call failed.');
    } finally {
      this.apiLoading.set(false);
      this.loadingMore.set(false);
    }
  }

  private async ensureCompaniesLoaded(): Promise<void> {
    if (
      !this.apiAuthContext.account() ||
      this.hasLoadedCompanies() ||
      this.companies().length > 0 ||
      this.isCompaniesLoading()
    ) {
      return;
    }

    await this.loadCompaniesPage(1, { reset: true });
  }

  private async loadCompanyAddresses(company: CompanyRow): Promise<void> {
    if (!this.apiAuthContext.account() || !company.id) {
      return;
    }

    const requestVersion = ++this.addressLoadVersion;
    this.addressesLoading.set(true);

    try {
      const addresses = await this.companyApi.getCompanyAddresses(company.id);

      if (requestVersion !== this.addressLoadVersion) {
        return;
      }

      this.companyAddresses.set(addresses);
      this.addressesLoaded.set(true);
    } catch (error) {
      if (requestVersion === this.addressLoadVersion) {
        this.addressError.set(error instanceof Error ? error.message : 'Address load failed.');
      }
    } finally {
      if (requestVersion === this.addressLoadVersion) {
        this.addressesLoading.set(false);
      }
    }
  }

  private toCreateCompanyRequest(): CreateCompanyRequest | null {
    if (!this.createCompanyForm.name.trim() || !this.createCompanyForm.status.trim()) {
      this.createCompanyError.set('Name and status are required.');
      return null;
    }

    return {
      name: this.createCompanyForm.name.trim(),
      externalCode: this.optionalTrimmed(this.createCompanyForm.externalCode),
      status: this.createCompanyForm.status.trim(),
    };
  }

  private toCreateAddressRequest(): CreateCompanyAddressRequest | null {
    if (
      !this.createAddressForm.addressType.trim() ||
      !this.createAddressForm.countryCode.trim() ||
      !this.createAddressForm.city.trim() ||
      !this.createAddressForm.line1.trim()
    ) {
      this.createAddressError.set('Address type, country code, city, and line 1 are required.');
      return null;
    }

    return {
      addressType: this.createAddressForm.addressType.trim(),
      countryCode: this.createAddressForm.countryCode.trim().toUpperCase(),
      city: this.createAddressForm.city.trim(),
      postalCode: this.optionalTrimmed(this.createAddressForm.postalCode),
      line1: this.createAddressForm.line1.trim(),
      line2: this.optionalTrimmed(this.createAddressForm.line2),
    };
  }

  private optionalTrimmed(value: string): string | null {
    const trimmed = value.trim();
    return trimmed.length > 0 ? trimmed : null;
  }

  private resetCreateCompanyForm(): void {
    this.createCompanyForm.name = '';
    this.createCompanyForm.externalCode = '';
    this.createCompanyForm.status = 'Active';
  }

  private resetCreateAddressForm(): void {
    this.createAddressForm.addressType = 'Shipping';
    this.createAddressForm.countryCode = '';
    this.createAddressForm.city = '';
    this.createAddressForm.postalCode = '';
    this.createAddressForm.line1 = '';
    this.createAddressForm.line2 = '';
  }

  private formatError(error: unknown, fallback: string): string {
    return formatProblemError(error, fallback);
  }

  private resetCompaniesPageState(): void {
    this.companies.set([]);
    this.selectedCompany.set(null);
    this.companyAddresses.set([]);
    this.addressesLoaded.set(false);
    this.addressError.set('');
    this.hasLoadedCompanies.set(false);
    this.currentCompaniesPage.set(0);
    this.companiesPageSize.set(0);
    this.hasMoreCompanies.set(false);
  }

  private applyCompaniesPage(
    items: CompanyRow[],
    page: { pageNumber: number; pageSize: number; hasMore: boolean; reset: boolean }
  ): void {
    this.companies.set(page.reset ? items : [...this.companies(), ...items]);
    this.currentCompaniesPage.set(page.pageNumber);
    this.companiesPageSize.set(page.pageSize);
    this.hasMoreCompanies.set(page.hasMore);
  }

  private initializeCompaniesObserver(): void {
    if (!this.viewReady || !this.sentinelElement || !('IntersectionObserver' in window)) {
      return;
    }

    this.observer?.disconnect();
    this.observer = new IntersectionObserver(
      (entries) => {
        const isVisible = entries.some((entry) => entry.isIntersecting);
        if (isVisible && this.hasLoadedCompanies() && this.hasMoreCompanies()) {
          void this.loadMoreCompanies();
        }
      },
      { rootMargin: '240px 0px' }
    );
    this.observer.observe(this.sentinelElement);
  }
}
