import { CommonModule } from '@angular/common';
import { Component, OnInit, computed, signal } from '@angular/core';
import {
  AccountInfo,
  InteractionRequiredAuthError,
  PublicClientApplication,
} from '@azure/msal-browser';
import { environment } from '../environments/environment';
import { loginRequest, msalConfig, tokenRequest } from './auth-config';

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
  protected readonly apiLoading = signal(false);
  protected readonly apiResult = signal<string>('');
  protected readonly apiError = signal<string>('');
  protected readonly isSignedIn = computed(() => this.account() !== null);

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

      this.apiResult.set(this.formatResponse(body));
    } catch (error) {
      this.apiError.set(error instanceof Error ? error.message : 'Gateway call failed.');
    } finally {
      this.apiLoading.set(false);
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

  private formatResponse(body: string): string {
    if (!body) {
      return '(empty response)';
    }

    try {
      return JSON.stringify(JSON.parse(body), null, 2);
    } catch {
      return body;
    }
  }
}
