import { AuthReturnUrlStore } from './auth-return-url-store';

describe('AuthReturnUrlStore', () => {
  let store: AuthReturnUrlStore;

  beforeEach(() => {
    store = new AuthReturnUrlStore();
    sessionStorage.clear();
  });

  afterEach(() => {
    sessionStorage.clear();
  });

  it('accepts protected local feature URLs with query strings', () => {
    expect(store.sanitize('/companies?page=2')).toBe('/companies?page=2');
    expect(store.sanitize('/inventory/items?sku=ABC-123')).toBe('/inventory/items?sku=ABC-123');
    expect(store.sanitize('/shipments/SHIP-1#details')).toBe('/shipments/SHIP-1#details');
  });

  it('rejects unsafe or unsupported return URLs', () => {
    expect(store.sanitize('https://example.com/companies')).toBeNull();
    expect(store.sanitize('//example.com/companies')).toBeNull();
    expect(store.sanitize('javascript:alert(1)')).toBeNull();
    expect(store.sanitize('/admin')).toBeNull();
    expect(store.sanitize('/sign-in')).toBeNull();
    expect(store.sanitize(null)).toBeNull();
  });

  it('stores, consumes, and clears a safe return URL once', () => {
    expect(store.store('/shipments?status=pending')).toBe(true);

    expect(store.consume()).toBe('/shipments?status=pending');
    expect(store.consume()).toBeNull();
  });

  it('clears stored values when an invalid return URL is stored', () => {
    expect(store.store('/inventory')).toBe(true);

    expect(store.store('https://example.com/inventory')).toBe(false);
    expect(store.consume()).toBeNull();
  });
});
