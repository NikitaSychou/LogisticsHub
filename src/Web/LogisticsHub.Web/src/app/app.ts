import { CommonModule } from '@angular/common';
import { Component, OnInit, computed, signal } from '@angular/core';
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

@Component({
  selector: 'app-root',
  imports: [CommonModule],
  templateUrl: './app.html',
  styleUrl: './app.css'
})
export class App implements OnInit {
  private readonly msal = new PublicClientApplication(msalConfig);

  protected readonly account = signal<AccountInfo | null>(null);
  protected readonly loading = signal(true);
  protected readonly activePage = signal<AppPage>('companies');
  protected readonly apiLoading = signal(false);
  protected readonly apiResult = signal<string>('');
  protected readonly apiError = signal<string>('');
  protected readonly companies = signal<CompanyRow[]>([]);
  protected readonly hasLoadedCompanies = signal(false);
  protected readonly isSignedIn = computed(() => this.account() !== null);
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
    const account = this.account();
    if (!account) {
      this.apiError.set('Sign in before calling the Gateway.');
      return;
    }

    this.apiLoading.set(true);
    this.apiResult.set('');
    this.apiError.set('');
    this.companies.set([]);
    this.hasLoadedCompanies.set(false);

    try {
      const token = await this.acquireAccessToken(account);
      const response = await fetch(`${environment.api.gatewayBaseUrl}/company/companies`, {
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
      this.companies.set(this.extractCompanies(parsed));
      this.apiResult.set(this.formatResponse(parsed, body));
      this.hasLoadedCompanies.set(true);
    } catch (error) {
      this.apiError.set(error instanceof Error ? error.message : 'Gateway call failed.');
    } finally {
      this.apiLoading.set(false);
    }
  }

  protected showPage(page: AppPage): void {
    this.activePage.set(page);
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

  private extractCompanies(payload: unknown): CompanyRow[] {
    const items = this.extractItems(payload);

    return items.map((item) => {
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

  private extractItems(payload: unknown): unknown[] {
    if (Array.isArray(payload)) {
      return payload;
    }

    const record = this.asRecord(payload);
    for (const key of ['items', 'data', 'results', 'companies']) {
      const value = record[key];
      if (Array.isArray(value)) {
        return value;
      }
    }

    return [];
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
}
