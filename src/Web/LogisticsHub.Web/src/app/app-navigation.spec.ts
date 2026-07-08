import { navigationItems } from './app-navigation';

describe('navigationItems', () => {
  it('defines the visible feature navigation labels and paths at app level', () => {
    expect(navigationItems).toEqual([
      { id: 'companies', label: 'Companies', path: '/companies' },
      { id: 'inventory', label: 'Inventory', path: '/inventory' },
      { id: 'shipments', label: 'Shipments', path: '/shipments' },
    ]);
  });
});
