import { CommonModule } from '@angular/common';
import { Component, EventEmitter, Input, Output } from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { NavigationItem } from '../navigation/navigation-item.model';

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

  @Output() login = new EventEmitter<void>();
  @Output() logout = new EventEmitter<void>();
}
