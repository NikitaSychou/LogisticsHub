import { Routes } from '@angular/router';

export const companiesRoutes: Routes = [
  {
    path: '',
    loadComponent: () => import('./companies-page').then((component) => component.CompaniesPage),
  },
];
