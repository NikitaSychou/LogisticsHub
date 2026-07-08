import { TestBed } from '@angular/core/testing';
import { AccountInfo } from '@azure/msal-browser';
import { ActivatedRouteSnapshot, Router, RouterStateSnapshot, UrlTree, convertToParamMap, provideRouter } from '@angular/router';
import { authReturnUrlPolicy } from './app-route-policy';
import { provideAuthReturnUrlPolicy } from './core/auth/auth-return-url-store';
import { signedInGuard, signedOutGuard } from './core/auth/signed-in.guard';
import { ApiAuthContext } from './core/http/api-auth-context';

describe('app auth route guards', () => {
  let authContext: ApiAuthContext;
  let router: Router;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideRouter([]), provideAuthReturnUrlPolicy(authReturnUrlPolicy)],
    });

    authContext = TestBed.inject(ApiAuthContext);
    router = TestBed.inject(Router);
  });

  it('redirects signed-out protected route access to sign-in with a safe returnUrl', async () => {
    authContext.configure(null, async () => 'access-token');

    const result = await runSignedInGuard('/companies?page=2');

    expectUrlTree(result, '/sign-in?returnUrl=%2Fcompanies%3Fpage%3D2');
  });

  it('does not preserve unsafe returnUrl values for signed-out protected access', async () => {
    authContext.configure(null, async () => 'access-token');

    const result = await runSignedInGuard('https://example.com/companies');

    expectUrlTree(result, '/sign-in');
  });

  it('allows signed-in protected route access', async () => {
    authContext.configure(account(), async () => 'access-token');

    await expect(runSignedInGuard('/shipments')).resolves.toBe(true);
  });

  it('allows signed-out users to stay on sign-in', async () => {
    authContext.configure(null, async () => 'access-token');

    await expect(runSignedOutGuard()).resolves.toBe(true);
  });

  it('redirects signed-in sign-in visits to a safe returnUrl', async () => {
    authContext.configure(account(), async () => 'access-token');

    const result = await runSignedOutGuard('/shipments');

    expectUrlTree(result, '/shipments');
  });

  it('redirects signed-in sign-in visits with invalid returnUrl to the default protected route', async () => {
    authContext.configure(account(), async () => 'access-token');

    const result = await runSignedOutGuard('https://example.com/shipments');

    expectUrlTree(result, '/companies');
  });

  it('redirects signed-in sign-in visits without returnUrl to the default protected route', async () => {
    authContext.configure(account(), async () => 'access-token');

    const result = await runSignedOutGuard();

    expectUrlTree(result, '/companies');
  });

  async function runSignedInGuard(url: string): Promise<ReturnType<typeof signedInGuard>> {
    return TestBed.runInInjectionContext(() => signedInGuard(routeSnapshot(), routerState(url)));
  }

  async function runSignedOutGuard(returnUrl?: string): Promise<ReturnType<typeof signedOutGuard>> {
    return TestBed.runInInjectionContext(() => signedOutGuard(routeSnapshot(returnUrl), routerState('/sign-in')));
  }

  function expectUrlTree(result: unknown, expectedUrl: string): void {
    expect(result).toBeInstanceOf(UrlTree);
    expect(router.serializeUrl(result as UrlTree)).toBe(expectedUrl);
  }
});

function routeSnapshot(returnUrl?: string): ActivatedRouteSnapshot {
  return {
    queryParamMap: convertToParamMap(returnUrl === undefined ? {} : { returnUrl }),
  } as ActivatedRouteSnapshot;
}

function routerState(url: string): RouterStateSnapshot {
  return { url } as RouterStateSnapshot;
}

function account(): AccountInfo {
  return {
    homeAccountId: 'home-account',
    environment: 'login.microsoftonline.com',
    tenantId: 'tenant',
    username: 'operator@example.com',
    localAccountId: 'local-account',
    name: 'Operator',
  };
}
