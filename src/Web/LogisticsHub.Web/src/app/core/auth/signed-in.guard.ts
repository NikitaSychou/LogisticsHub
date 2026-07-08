import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthReturnUrlStore } from './auth-return-url-store';
import { ApiAuthContext } from '../http/api-auth-context';

export const signedInGuard: CanActivateFn = async (_route, state) => {
  const authContext = inject(ApiAuthContext);
  const returnUrlStore = inject(AuthReturnUrlStore);
  const router = inject(Router);

  await authContext.whenReady();

  if (authContext.account()) {
    return true;
  }

  const returnUrl = returnUrlStore.sanitize(state.url);
  return router.createUrlTree(['/sign-in'], {
    queryParams: returnUrl ? { returnUrl } : undefined,
  });
};

export const signedOutGuard: CanActivateFn = async (route) => {
  const authContext = inject(ApiAuthContext);
  const returnUrlStore = inject(AuthReturnUrlStore);
  const router = inject(Router);

  await authContext.whenReady();

  if (!authContext.account()) {
    return true;
  }

  return router.parseUrl(returnUrlStore.sanitize(route.queryParamMap.get('returnUrl')) ?? '/companies');
};
