import { Routes } from '@angular/router';

export const companiesRoutes: Routes = [
  {
    path: '',
    loadComponent: () => import('./pages/companies-page/companies-page').then((component) => component.CompaniesPage),
  },
];
