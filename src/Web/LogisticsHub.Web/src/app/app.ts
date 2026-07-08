import { Component, OnInit, computed, inject, signal } from '@angular/core';
import {
  AccountInfo,
  InteractionRequiredAuthError,
  PublicClientApplication,
} from '@azure/msal-browser';
import { environment } from '../environments/environment';
import { loginRequest, msalConfig, tokenRequest } from './auth-config';
import { AuthReturnUrlStore } from './core/auth/auth-return-url-store';
import { ApiAuthContext } from './core/http/api-auth-context';
import { AppShell } from './core/layout/app-shell';
import { navigationItems } from './core/navigation/navigation-items';
import { Router } from '@angular/router';

@Component({
  selector: 'app-root',
  imports: [AppShell],
  templateUrl: './app.html',
  styleUrl: './app.css',
})
export class App implements OnInit {
  private readonly msal = new PublicClientApplication(msalConfig);
  private readonly apiAuthContext = inject(ApiAuthContext);
  private readonly returnUrlStore = inject(AuthReturnUrlStore);
  private readonly router = inject(Router);

  protected readonly account = signal<AccountInfo | null>(null);
  protected readonly loading = signal(true);
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
    this.apiAuthContext.configure(activeAccount, this.getAccessToken);
    this.loading.set(false);

    if (activeAccount) {
      await this.navigateToStoredReturnUrl();
    }
  }

  protected async login(): Promise<void> {
    this.storeReturnUrlForLogin();
    await this.msal.loginRedirect(loginRequest);
  }

  protected async logout(): Promise<void> {
    await this.msal.logoutRedirect({
      account: this.account(),
      postLogoutRedirectUri: environment.msal.redirectUri,
    });
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

  private storeReturnUrlForLogin(): void {
    const returnUrl = this.router.parseUrl(this.router.url).queryParams['returnUrl'];
    if (!this.returnUrlStore.store(returnUrl)) {
      this.returnUrlStore.clear();
    }
  }

  private async navigateToStoredReturnUrl(): Promise<void> {
    const returnUrl = this.returnUrlStore.consume();
    if (!returnUrl || this.router.url === returnUrl) {
      return;
    }

    await this.router.navigateByUrl(returnUrl);
  }
}
