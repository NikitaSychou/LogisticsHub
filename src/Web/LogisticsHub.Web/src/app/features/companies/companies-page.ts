import { CommonModule } from '@angular/common';
import {
  AfterViewInit,
  Component,
  ElementRef,
  Input,
  OnChanges,
  OnDestroy,
  SimpleChanges,
  ViewChild,
  computed,
  inject,
  signal,
} from '@angular/core';
import { AccountInfo } from '@azure/msal-browser';
import { PagedResponse } from '../../shared/models/paged-response';
import { CompanyApiService } from './company-api.service';
import {
  CompanyAddressRow,
  CompanyRow,
  CreateCompanyAddressRequest,
  CreateCompanyRequest,
} from './company.models';

@Component({
  selector: 'app-companies-page',
  imports: [CommonModule],
  templateUrl: './companies-page.html',
  styleUrl: './companies-page.css',
})
export class CompaniesPage implements AfterViewInit, OnChanges, OnDestroy {
  private readonly companyApi = inject(CompanyApiService);
  private observer?: IntersectionObserver;
  private sentinelElement?: HTMLElement;
  private viewReady = false;
  private addressLoadVersion = 0;

  @Input({ required: true }) accessTokenFactory!: () => Promise<string>;
  @Input({ required: true }) account!: AccountInfo | null;
  @Input({ required: true }) active = false;

  @ViewChild('companiesScrollSentinel')
  private set companiesScrollSentinel(value: ElementRef<HTMLElement> | undefined) {
    this.sentinelElement = value?.nativeElement;
    this.initializeCompaniesObserver();
  }

  protected readonly apiLoading = signal(false);
  protected readonly loadingMore = signal(false);
  protected readonly apiResult = signal('');
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

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['active'] || changes['account']) {
      void this.ensureCompaniesLoaded();
    }
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

  protected isSelectedCompany(company: CompanyRow): boolean {
    return company.id !== undefined && this.selectedCompany()?.id === company.id;
  }

  protected inputValue(event: Event): string {
    return event.target instanceof HTMLInputElement || event.target instanceof HTMLSelectElement
      ? event.target.value
      : '';
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
      const body = await this.companyApi.createCompany(request, await this.accessTokenFactory());
      const createdCompany = this.extractCompanies([this.parseBody(body)])[0] ?? null;

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
      await this.companyApi.createCompanyAddress(company.id, request, await this.accessTokenFactory());
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

    if (!this.account) {
      this.apiError.set('Sign in before calling the Gateway.');
      return;
    }

    this.apiLoading.set(options.reset);
    this.loadingMore.set(!options.reset);
    this.apiError.set('');

    if (options.reset) {
      this.apiResult.set('');
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

    try {
      const body = await this.companyApi.getCompaniesPage(pageNumber, await this.accessTokenFactory());
      const parsed = this.parseBody(body);
      const page = this.toPagedCompanies(parsed, pageNumber);
      this.companies.set(options.reset ? page.items : [...this.companies(), ...page.items]);
      this.currentCompaniesPage.set(page.pageNumber);
      this.companiesPageSize.set(page.pageSize);
      this.hasMoreCompanies.set(page.hasMore);
      this.apiResult.set(this.formatResponse(parsed, body));
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
      !this.active ||
      !this.account ||
      this.hasLoadedCompanies() ||
      this.companies().length > 0 ||
      this.isCompaniesLoading()
    ) {
      return;
    }

    await this.loadCompaniesPage(1, { reset: true });
  }

  private async loadCompanyAddresses(company: CompanyRow): Promise<void> {
    if (!this.account || !company.id) {
      return;
    }

    const requestVersion = ++this.addressLoadVersion;
    this.addressesLoading.set(true);

    try {
      const body = await this.companyApi.getCompanyAddresses(company.id, await this.accessTokenFactory());

      if (requestVersion !== this.addressLoadVersion) {
        return;
      }

      const parsed = this.parseBody(body);
      const rows = Array.isArray(parsed) ? parsed : [];
      this.companyAddresses.set(this.extractAddresses(rows));
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

  private parseBody(body: string): unknown {
    if (!body) {
      return null;
    }

    try {
      return JSON.parse(body);
    } catch {
      return body;
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
    if (!(error instanceof Error)) {
      return fallback;
    }

    const problem = this.tryExtractProblemDetails(error.message);
    return problem ?? error.message;
  }

  private tryExtractProblemDetails(message: string): string | null {
    const jsonStart = message.indexOf('{');
    if (jsonStart < 0) {
      return null;
    }

    const parsed = this.parseBody(message.substring(jsonStart));
    const record = this.asRecord(parsed);
    const errors = this.asRecord(record['errors']);
    const details = Object.entries(errors)
      .flatMap(([field, value]) =>
        Array.isArray(value)
          ? value.map((item) => `${field}: ${String(item)}`)
          : []
      );

    return details.length > 0 ? details.join('\n') : null;
  }

  private formatResponse(parsed: unknown, rawBody: string): string {
    if (!rawBody) {
      return '(empty response)';
    }

    return typeof parsed === 'string' ? parsed : JSON.stringify(parsed, null, 2);
  }

  private extractCompanies(payload: unknown[]): CompanyRow[] {
    return payload.map((item) => {
      const record = this.asRecord(item);

      return {
        id: this.stringValue(record, ['id', 'companyId']),
        externalCode: this.stringValue(record, ['externalCode', 'code']),
        name: this.stringValue(record, ['name', 'companyName']),
        status: this.stringValue(record, ['status']),
        createdAtUtc: this.stringValue(record, ['createdAtUtc']),
        updatedAtUtc: this.stringValue(record, ['updatedAtUtc']),
        raw: item,
      };
    });
  }

  private extractAddresses(payload: unknown[]): CompanyAddressRow[] {
    return payload.map((item) => {
      const record = this.asRecord(item);

      return {
        id: this.stringValue(record, ['id', 'addressId']),
        addressType: this.stringValue(record, ['addressType', 'type']),
        countryCode: this.stringValue(record, ['countryCode']),
        city: this.stringValue(record, ['city']),
        postalCode: this.stringValue(record, ['postalCode']),
        line1: this.stringValue(record, ['line1']),
        line2: this.stringValue(record, ['line2']),
        createdAtUtc: this.stringValue(record, ['createdAtUtc']),
        updatedAtUtc: this.stringValue(record, ['updatedAtUtc']),
      };
    });
  }

  private toPagedCompanies(payload: unknown, requestedPage: number): PagedResponse<CompanyRow> {
    const record = this.asRecord(payload);
    const rawItems = Array.isArray(record['items']) ? record['items'] : [];

    return {
      items: this.extractCompanies(rawItems),
      pageNumber: this.numberValue(record, 'pageNumber') ?? requestedPage,
      pageSize: this.numberValue(record, 'pageSize') ?? rawItems.length,
      hasMore: this.booleanValue(record, 'hasMore') ?? false,
    };
  }

  private asRecord(value: unknown): Record<string, unknown> {
    return value !== null && typeof value === 'object' ? (value as Record<string, unknown>) : {};
  }

  private stringValue(record: Record<string, unknown>, keys: string[]): string | undefined {
    for (const key of keys) {
      const value = record[key];
      if (typeof value === 'string' && value.trim().length > 0) {
        return value;
      }
    }

    return undefined;
  }

  private numberValue(record: Record<string, unknown>, key: string): number | undefined {
    const value = record[key];
    return typeof value === 'number' && Number.isFinite(value) ? value : undefined;
  }

  private booleanValue(record: Record<string, unknown>, key: string): boolean | undefined {
    const value = record[key];
    return typeof value === 'boolean' ? value : undefined;
  }

  private initializeCompaniesObserver(): void {
    if (!this.viewReady || !this.sentinelElement || !('IntersectionObserver' in window)) {
      return;
    }

    this.observer?.disconnect();
    this.observer = new IntersectionObserver(
      (entries) => {
        const isVisible = entries.some((entry) => entry.isIntersecting);
        if (isVisible && this.active && this.hasLoadedCompanies() && this.hasMoreCompanies()) {
          void this.loadMoreCompanies();
        }
      },
      { rootMargin: '240px 0px' }
    );
    this.observer.observe(this.sentinelElement);
  }
}
