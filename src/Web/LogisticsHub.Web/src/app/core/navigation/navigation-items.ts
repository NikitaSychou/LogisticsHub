import { NavigationItem } from './navigation-item.model';

export const navigationItems: readonly NavigationItem[] = [
  { id: 'companies', label: 'Companies', path: '/companies' },
  { id: 'inventory', label: 'Inventory', path: '/inventory' },
  { id: 'shipments', label: 'Shipments', path: '/shipments' },
];
