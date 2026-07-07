export type AppPage = 'companies' | 'inventory' | 'shipments';

export interface NavigationItem {
  readonly id: AppPage;
  readonly label: string;
}
