import { ApiAuthContext } from './api-auth-context';

describe('ApiAuthContext', () => {
  it('throws a clear error before an access token factory is configured', async () => {
    const context = new ApiAuthContext();

    await expect(context.getAccessToken()).rejects.toThrow('Sign in before calling the Gateway.');
  });

  it('resolves readiness and exposes the configured account', async () => {
    const context = new ApiAuthContext();
    const ready = context.whenReady();
    const account = {
      homeAccountId: 'home-account',
      environment: 'login.microsoftonline.com',
      tenantId: 'tenant',
      username: 'operator@example.com',
      localAccountId: 'local-account',
      name: 'Operator',
    };

    context.configure(account, async () => 'access-token');

    await ready;

    expect(context.initialized()).toBe(true);
    expect(context.account()).toBe(account);
    await expect(context.getAccessToken()).resolves.toBe('access-token');
  });
});
