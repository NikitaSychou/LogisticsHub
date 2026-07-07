import { CommonModule } from '@angular/common';
import {
  AfterViewInit,
  Component,
  ElementRef,
  OnDestroy,
  OnInit,
  ViewChild,
  computed,
  signal,
} from '@angular/core';
import {
  AccountInfo,
  InteractionRequiredAuthError,
  PublicClientApplication,
} from '@azure/msal-browser';
import { environment } from '../environments/environment';
import { loginRequest, msalConfig, tokenRequest } from './auth-config';

type AppPage = 'companies' | 'inventory' | 'shipments';

interface CompanyRow {
  id?: string;
  externalCode?: string;
  name?: string;
  status?: string;
  raw: unknown;
}

interface PagedResponse<T> {
  items: T[];
  pageNumber: number;
  pageSize: number;
  hasMore: boolean;
}

@Component({
  selector: 'app-root',
  imports: [CommonModule],
  templateUrl: './app.html',
  styleUrl: './app.css'
})
export class App implements OnInit, AfterViewInit, OnDestroy {
  private readonly msal = new PublicClientApplication(msalConfig);
  private observer?: IntersectionObserver;
  private sentinelElement?: HTMLElement;
  private viewReady = false;

  @ViewChild('companiesScrollSentinel')
  private set companiesScrollSentinel(value: ElementRef<HTMLElement> | undefined) {
    this.sentinelElement = value?.nativeElement;
    this.initializeCompaniesObserver();
  }

  protected readonly account = signal<AccountInfo | null>(null);
  protected readonly loading = signal(true);
  protected readonly activePage = signal<AppPage>('companies');
  protected readonly apiLoading = signal(false);
  protected readonly loadingMore = signal(false);
  protected readonly apiResult = signal<string>('');
  protected readonly apiError = signal<string>('');
  protected readonly companies = signal<CompanyRow[]>([]);
  protected readonly hasLoadedCompanies = signal(false);
  protected readonly currentCompaniesPage = signal(0);
  protected readonly companiesPageSize = signal(0);
  protected readonly hasMoreCompanies = signal(false);
  protected readonly isSignedIn = computed(() => this.account() !== null);
  protected readonly isCompaniesLoading = computed(() => this.apiLoading() || this.loadingMore());
  protected readonly signedInName = computed(() => this.account()?.name ?? this.account()?.username ?? '');
  protected readonly pageTitle = computed(() => {
    switch (this.activePage()) {
      case 'inventory':
        return 'Inventory';
      case 'shipments':
        return 'Shipments';
      default:
        return 'Companies';
    }
  });

  async ngOnInit(): Promise<void> {
    await this.msal.initialize();

    const response = await this.msal.handleRedirectPromise();
    if (response?.account) {
      this.msal.setActiveAccount(response.account);
    }

    const activeAccount = this.msal.getActiveAccount() ?? this.msal.getAllAccounts()[0] ?? null;
    if (activeAccount) {
      this.msal.setActiveAccount(activeAccount);
    }

    this.account.set(activeAccount);
    this.loading.set(false);
  }

  ngAfterViewInit(): void {
    this.viewReady = true;
    this.initializeCompaniesObserver();
  }

  ngOnDestroy(): void {
    this.observer?.disconnect();
  }

  protected async login(): Promise<void> {
    await this.msal.loginRedirect(loginRequest);
  }

  protected async logout(): Promise<void> {
    await this.msal.logoutRedirect({
      account: this.account(),
      postLogoutRedirectUri: environment.msal.redirectUri,
    });
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

  protected showPage(page: AppPage): void {
    this.activePage.set(page);
  }

  private async loadCompaniesPage(pageNumber: number, options: { reset: boolean }): Promise<void> {
    if (this.isCompaniesLoading()) {
      return;
    }

    const account = this.account();
    if (!account) {
      this.apiError.set('Sign in before calling the Gateway.');
      return;
    }

    this.apiLoading.set(options.reset);
    this.loadingMore.set(!options.reset);
    this.apiError.set('');

    if (options.reset) {
      this.apiResult.set('');
      this.companies.set([]);
      this.hasLoadedCompanies.set(false);
      this.currentCompaniesPage.set(0);
      this.companiesPageSize.set(0);
      this.hasMoreCompanies.set(false);
    }

    try {
      const token = await this.acquireAccessToken(account);
      const response = await fetch(`${environment.api.gatewayBaseUrl}/company/companies/page?pageNumber=${pageNumber}`, {
        headers: {
          Authorization: `Bearer ${token}`,
        },
      });
      const body = await response.text();

      if (!response.ok) {
        this.apiError.set(`Gateway returned ${response.status}: ${body || response.statusText}`);
        return;
      }

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

  private async acquireAccessToken(account: AccountInfo): Promise<string> {
    try {
      const result = await this.msal.acquireTokenSilent(tokenRequest(account));
      return result.accessToken;
    } catch (error) {
      if (error instanceof InteractionRequiredAuthError) {
        const result = await this.msal.acquireTokenPopup(loginRequest);
        return result.accessToken;
      }

      throw error;
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
        raw: item,
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
        if (isVisible && this.activePage() === 'companies' && this.hasLoadedCompanies() && this.hasMoreCompanies()) {
          void this.loadMoreCompanies();
        }
      },
      { rootMargin: '240px 0px' }
    );
    this.observer.observe(this.sentinelElement);
  }
}
