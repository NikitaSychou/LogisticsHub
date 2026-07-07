import {
  BrowserCacheLocation,
  type Configuration,
  type RedirectRequest,
  type SilentRequest,
} from '@azure/msal-browser';
import { environment } from '../environments/environment';

export const msalConfig: Configuration = {
  auth: {
    clientId: environment.msal.clientId,
    authority: environment.msal.authority,
    redirectUri: environment.msal.redirectUri,
  },
  cache: {
    cacheLocation: BrowserCacheLocation.LocalStorage,
  },
};

export const loginRequest: RedirectRequest = {
  scopes: [environment.api.scope],
};

export const tokenRequest = (account: SilentRequest['account']): SilentRequest => ({
  account,
  scopes: [environment.api.scope],
});
