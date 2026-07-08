import { bootstrapApplication } from '@angular/platform-browser';
import { loadRuntimeConfig } from './app/core/config/runtime-config';

loadRuntimeConfig()
  .then(async () => {
    const [{ appConfig }, { App }] = await Promise.all([import('./app/app.config'), import('./app/app')]);
    return bootstrapApplication(App, appConfig);
  })
  .catch((err) => console.error(err));
