import {
  BrowserCacheLocation,
  type Configuration,
  type RedirectRequest,
  type SilentRequest,
} from '@azure/msal-browser';
import { runtimeConfig } from './core/config/runtime-config';

const config = runtimeConfig();

export const msalConfig: Configuration = {
  auth: {
    clientId: config.msal.clientId,
    authority: config.msal.authority,
    redirectUri: config.msal.redirectUri,
  },
  cache: {
    cacheLocation: BrowserCacheLocation.LocalStorage,
  },
};

export const loginRequest: RedirectRequest = {
  scopes: [config.api.scope],
};

export const tokenRequest = (account: SilentRequest['account']): SilentRequest => ({
  account,
  scopes: [config.api.scope],
});
