import { Routes } from '@angular/router';

export const routes: Routes = [
  {
    path: 'companies',
    loadChildren: () => import('./features/companies/companies.routes').then((routes) => routes.companiesRoutes),
  },
  {
    path: 'inventory',
    loadChildren: () => import('./features/inventory/inventory.routes').then((routes) => routes.inventoryRoutes),
  },
  {
    path: 'shipments',
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
