import { AuthReturnUrlPolicy } from './core/auth/auth-return-url-store';

export const authReturnUrlPolicy: AuthReturnUrlPolicy = {
  allowedProtectedPaths: ['/companies', '/inventory', '/shipments'],
  defaultProtectedPath: '/companies',
};
