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
import { CompanyApiService } from './company-api.service';
import { CompanyAddressRow, CompanyRow, PagedResponse } from './company.models';

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
  protected readonly hasLoadedCompanies = signal(false);
  protected readonly currentCompaniesPage = signal(0);
  protected readonly companiesPageSize = signal(0);
  protected readonly hasMoreCompanies = signal(false);
  protected readonly isCompaniesLoading = computed(() => this.apiLoading() || this.loadingMore());

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

    if (!company.id) {
      this.addressError.set('Cannot load addresses because the selected company has no ID.');
      return;
    }

    await this.loadCompanyAddresses(company);
  }

  protected isSelectedCompany(company: CompanyRow): boolean {
    return company.id !== undefined && this.selectedCompany()?.id === company.id;
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
