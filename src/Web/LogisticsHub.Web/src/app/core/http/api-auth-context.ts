import { Injectable, signal } from '@angular/core';
import { AccountInfo } from '@azure/msal-browser';

@Injectable({ providedIn: 'root' })
export class ApiAuthContext {
  private readonly accountSignal = signal<AccountInfo | null>(null);
  private accessTokenFactory?: () => Promise<string>;

  readonly account = this.accountSignal.asReadonly();

  configure(account: AccountInfo | null, accessTokenFactory: () => Promise<string>): void {
    this.accountSignal.set(account);
    this.accessTokenFactory = accessTokenFactory;
  }

  async getAccessToken(): Promise<string> {
    if (!this.accessTokenFactory) {
      throw new Error('Sign in before calling the Gateway.');
    }

    return this.accessTokenFactory();
  }
}
