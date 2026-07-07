import { CommonModule } from '@angular/common';
import { Component, EventEmitter, Input, Output } from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { AccountInfo } from '@azure/msal-browser';
import { NavigationItem } from '../navigation/navigation-item.model';

type RoutedFeaturePage = {
  account?: AccountInfo | null;
  accessTokenFactory?: () => Promise<string>;
  active?: boolean;
  loadCompanies?: () => Promise<void>;
  loadInventory?: () => Promise<void>;
};

@Component({
  selector: 'app-shell',
  imports: [CommonModule, RouterLink, RouterLinkActive, RouterOutlet],
  templateUrl: './app-shell.html',
  styleUrl: './app-shell.css',
})
export class AppShell {
  @Input({ required: true }) loading = true;
  @Input({ required: true }) isSignedIn = false;
  @Input({ required: true }) signedInName = '';
  @Input({ required: true }) navigationItems: readonly NavigationItem[] = [];
  @Input({ required: true }) account!: AccountInfo | null;
  @Input({ required: true }) accessTokenFactory!: () => Promise<string>;

  @Output() login = new EventEmitter<void>();
  @Output() logout = new EventEmitter<void>();

  protected configureFeaturePage(component: unknown): void {
    const page = component as RoutedFeaturePage;
    page.account = this.account;
    page.accessTokenFactory = this.accessTokenFactory;
    page.active = true;

    void page.loadCompanies?.();
    void page.loadInventory?.();
  }
}
