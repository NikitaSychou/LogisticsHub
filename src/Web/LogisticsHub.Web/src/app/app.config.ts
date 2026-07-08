import { ApplicationConfig, provideBrowserGlobalErrorListeners } from '@angular/core';
import { provideHttpClient } from '@angular/common/http';
import { provideRouter } from '@angular/router';
import { routes } from './app.routes';
import { authReturnUrlPolicy } from './app-route-policy';
import { provideAuthReturnUrlPolicy } from './core/auth/auth-return-url-store';

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideHttpClient(),
    provideAuthReturnUrlPolicy(authReturnUrlPolicy),
    provideRouter(routes),
  ],
};
