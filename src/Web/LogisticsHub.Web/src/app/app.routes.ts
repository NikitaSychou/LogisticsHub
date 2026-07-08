import { Routes } from '@angular/router';
import { signedInGuard, signedOutGuard } from './core/auth/signed-in.guard';

export const routes: Routes = [
  {
    path: 'sign-in',
    canActivate: [signedOutGuard],
    loadComponent: () => import('./core/auth/sign-in-page').then((component) => component.SignInPage),
  },
  {
    path: 'companies',
    canActivate: [signedInGuard],
    loadChildren: () => import('./features/companies/companies.routes').then((routes) => routes.companiesRoutes),
  },
  {
    path: 'inventory',
    canActivate: [signedInGuard],
    loadChildren: () => import('./features/inventory/inventory.routes').then((routes) => routes.inventoryRoutes),
  },
  {
    path: 'shipments',
    canActivate: [signedInGuard],
    loadChildren: () => import('./features/shipments/shipments.routes').then((routes) => routes.shipmentsRoutes),
  },
  {
    path: '',
    pathMatch: 'full',
    redirectTo: 'companies',
  },
  {
    path: '**',
    redirectTo: 'companies',
  },
];
