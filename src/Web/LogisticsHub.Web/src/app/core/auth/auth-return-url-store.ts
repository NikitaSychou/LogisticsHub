import { Injectable } from '@angular/core';

const RETURN_URL_STORAGE_KEY = 'logisticshub.auth.returnUrl';
const ALLOWED_PROTECTED_PATHS = ['/companies', '/inventory', '/shipments'];
const MAX_RETURN_URL_LENGTH = 2048;

@Injectable({ providedIn: 'root' })
export class AuthReturnUrlStore {
  sanitize(value: unknown): string | null {
    if (typeof value !== 'string') {
      return null;
    }

    const candidate = value.trim();
    if (
      candidate.length === 0 ||
      candidate.length > MAX_RETURN_URL_LENGTH ||
      !candidate.startsWith('/') ||
      candidate.startsWith('//') ||
      candidate.startsWith('/\\')
    ) {
      return null;
    }

    const path = candidate.split(/[?#]/, 1)[0];
    return ALLOWED_PROTECTED_PATHS.some((allowedPath) => path === allowedPath || path.startsWith(`${allowedPath}/`))
      ? candidate
      : null;
  }

  store(value: unknown): boolean {
    const sanitized = this.sanitize(value);
    if (!sanitized) {
      this.clear();
      return false;
    }

    try {
      sessionStorage.setItem(RETURN_URL_STORAGE_KEY, sanitized);
      return true;
    } catch {
      return false;
    }
  }

  consume(): string | null {
    let value: string | null = null;

    try {
      value = sessionStorage.getItem(RETURN_URL_STORAGE_KEY);
    } finally {
      this.clear();
    }

    return this.sanitize(value);
  }

  clear(): void {
    try {
      sessionStorage.removeItem(RETURN_URL_STORAGE_KEY);
    } catch {
      // Ignore storage failures; routing falls back to the default feature route.
    }
  }
}
