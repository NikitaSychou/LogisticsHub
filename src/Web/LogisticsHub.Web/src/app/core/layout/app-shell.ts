import { CommonModule } from '@angular/common';
import { Component, EventEmitter, Input, Output } from '@angular/core';
import { AccountInfo } from '@azure/msal-browser';
import { CompaniesPage } from '../../features/companies/companies-page';
import { InventoryPage } from '../../features/inventory/inventory-page';
import { ShipmentsPage } from '../../features/shipments/shipments-page';
import { AppPage, NavigationItem } from '../navigation/navigation-item.model';

@Component({
  selector: 'app-shell',
  imports: [CommonModule, CompaniesPage, InventoryPage, ShipmentsPage],
  templateUrl: './app-shell.html',
  styleUrl: './app-shell.css',
})
export class AppShell {
  @Input({ required: true }) loading = true;
  @Input({ required: true }) isSignedIn = false;
  @Input({ required: true }) signedInName = '';
  @Input({ required: true }) activePage!: AppPage;
  @Input({ required: true }) navigationItems: readonly NavigationItem[] = [];
  @Input({ required: true }) account!: AccountInfo | null;
  @Input({ required: true }) accessTokenFactory!: () => Promise<string>;

  @Output() login = new EventEmitter<void>();
  @Output() logout = new EventEmitter<void>();
  @Output() pageSelected = new EventEmitter<AppPage>();
}
