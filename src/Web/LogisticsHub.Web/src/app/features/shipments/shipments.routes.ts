import { Routes } from '@angular/router';

export const shipmentsRoutes: Routes = [
  {
    path: '',
    loadComponent: () => import('./shipments-page').then((component) => component.ShipmentsPage),
  },
];
