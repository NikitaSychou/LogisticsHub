import { Routes } from '@angular/router';

export const shipmentsRoutes: Routes = [
  {
    path: '',
    loadComponent: () => import('./pages/shipments-page/shipments-page').then((component) => component.ShipmentsPage),
  },
];
