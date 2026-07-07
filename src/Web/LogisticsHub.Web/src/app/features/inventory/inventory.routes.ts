import { Routes } from '@angular/router';

export const inventoryRoutes: Routes = [
  {
    path: '',
    loadComponent: () => import('./pages/inventory-page/inventory-page').then((component) => component.InventoryPage),
  },
];
