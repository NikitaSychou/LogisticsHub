import { Injectable, signal } from '@angular/core';
import { AccountInfo } from '@azure/msal-browser';

@Injectable({ providedIn: 'root' })
export class ApiAuthContext {
  private readonly accountSignal = signal<AccountInfo | null>(null);
  private readonly initializedSignal = signal(false);
  private accessTokenFactory?: () => Promise<string>;
  private resolveReady?: () => void;
  private readonly ready = new Promise<void>((resolve) => {
    this.resolveReady = resolve;
  });

  readonly account = this.accountSignal.asReadonly();
  readonly initialized = this.initializedSignal.asReadonly();

  configure(account: AccountInfo | null, accessTokenFactory: () => Promise<string>): void {
    this.accountSignal.set(account);
    this.accessTokenFactory = accessTokenFactory;

    if (!this.initializedSignal()) {
      this.initializedSignal.set(true);
      this.resolveReady?.();
    }
  }

  async getAccessToken(): Promise<string> {
    if (!this.accessTokenFactory) {
      throw new Error('Sign in before calling the Gateway.');
    }

    return this.accessTokenFactory();
  }

  async whenReady(): Promise<void> {
    if (this.initializedSignal()) {
      return;
    }

    await this.ready;
  }
}
