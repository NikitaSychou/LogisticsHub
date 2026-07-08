import { authReturnUrlPolicy } from './app-route-policy';

describe('authReturnUrlPolicy', () => {
  it('defines the app protected return URL routes and default fallback', () => {
    expect(authReturnUrlPolicy.allowedProtectedPaths).toEqual(['/companies', '/inventory', '/shipments']);
    expect(authReturnUrlPolicy.defaultProtectedPath).toBe('/companies');
  });
});
