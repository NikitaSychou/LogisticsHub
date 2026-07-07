import { CommonModule } from '@angular/common';
import { Component, EventEmitter, Input, Output } from '@angular/core';
import { EmptyState } from '../../../shared/ui/empty-state/empty-state';
import { InventoryItemRow } from '../inventory.models';

@Component({
  selector: 'app-inventory-list',
  imports: [CommonModule, EmptyState],
  templateUrl: './inventory-list.html',
  styleUrl: './inventory-list.css',
})
export class InventoryList {
  @Input({ required: true }) items: InventoryItemRow[] = [];
  @Input({ required: true }) selectedItem: InventoryItemRow | null = null;
  @Input({ required: true }) hasLoadedItems = false;
  @Input({ required: true }) apiLoading = false;
  @Input({ required: true }) apiError = '';

  @Output() itemSelected = new EventEmitter<InventoryItemRow>();

  protected get emptyStateMessage(): string {
    if (!this.hasLoadedItems && this.apiLoading) {
      return 'Loading inventory items...';
    }

    if (!this.hasLoadedItems && !this.apiLoading && !this.apiError) {
      return 'Inventory items will appear here when they are loaded.';
    }

    return 'The response did not contain any inventory item rows.';
  }

  protected isSelectedItem(item: InventoryItemRow): boolean {
    return item.sku !== undefined && this.selectedItem?.sku === item.sku;
  }
}
