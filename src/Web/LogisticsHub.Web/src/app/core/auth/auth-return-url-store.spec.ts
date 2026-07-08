import { TestBed } from '@angular/core/testing';
import { AuthReturnUrlStore, provideAuthReturnUrlPolicy } from './auth-return-url-store';

describe('AuthReturnUrlStore', () => {
  let store: AuthReturnUrlStore;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideAuthReturnUrlPolicy({
          allowedProtectedPaths: ['/protected', '/operations', '/reports'],
          defaultProtectedPath: '/protected',
        }),
      ],
    });

    store = TestBed.inject(AuthReturnUrlStore);
    sessionStorage.clear();
  });

  afterEach(() => {
    sessionStorage.clear();
  });

  it('accepts protected local feature URLs with query strings', () => {
    expect(store.sanitize('/protected?page=2')).toBe('/protected?page=2');
    expect(store.sanitize('/operations/items?code=ABC-123')).toBe('/operations/items?code=ABC-123');
    expect(store.sanitize('/reports/REPORT-1#details')).toBe('/reports/REPORT-1#details');
  });

  it('rejects unsafe or unsupported return URLs', () => {
    expect(store.sanitize('https://example.com/protected')).toBeNull();
    expect(store.sanitize('//example.com/protected')).toBeNull();
    expect(store.sanitize('javascript:alert(1)')).toBeNull();
    expect(store.sanitize('/admin')).toBeNull();
    expect(store.sanitize('/sign-in')).toBeNull();
    expect(store.sanitize(null)).toBeNull();
  });

  it('stores, consumes, and clears a safe return URL once', () => {
    expect(store.store('/reports?status=pending')).toBe(true);

    expect(store.consume()).toBe('/reports?status=pending');
    expect(store.consume()).toBeNull();
  });

  it('clears stored values when an invalid return URL is stored', () => {
    expect(store.store('/operations')).toBe(true);

    expect(store.store('https://example.com/operations')).toBe(false);
    expect(store.consume()).toBeNull();
  });

  it('exposes the configured default return URL', () => {
    expect(store.defaultReturnUrl).toBe('/protected');
  });
});
