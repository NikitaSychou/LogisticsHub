import { Component, OnInit, computed, signal } from '@angular/core';
import {
  AccountInfo,
  InteractionRequiredAuthError,
  PublicClientApplication,
} from '@azure/msal-browser';
import { environment } from '../environments/environment';
import { loginRequest, msalConfig, tokenRequest } from './auth-config';
import { AppShell } from './core/layout/app-shell';
import { AppPage } from './core/navigation/navigation-item.model';
import { navigationItems } from './core/navigation/navigation-items';

@Component({
  selector: 'app-root',
  imports: [AppShell],
  templateUrl: './app.html',
  styleUrl: './app.css',
})
export class App implements OnInit {
  private readonly msal = new PublicClientApplication(msalConfig);

  protected readonly account = signal<AccountInfo | null>(null);
  protected readonly loading = signal(true);
  protected readonly activePage = signal<AppPage>('companies');
  protected readonly navigationItems = navigationItems;
  protected readonly isSignedIn = computed(() => this.account() !== null);
  protected readonly signedInName = computed(() => this.account()?.name ?? this.account()?.username ?? '');

  protected readonly getAccessToken = async (): Promise<string> => {
    const account = this.account();
    if (!account) {
      throw new Error('Sign in before calling the Gateway.');
    }

    return this.acquireAccessToken(account);
  };

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
}
