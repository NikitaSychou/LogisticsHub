import { CommonModule } from '@angular/common';
import { Component, EventEmitter, Input, Output } from '@angular/core';
import { InventoryItemRow } from '../inventory.models';

@Component({
  selector: 'app-inventory-list',
  imports: [CommonModule],
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

  protected isSelectedItem(item: InventoryItemRow): boolean {
    return item.sku !== undefined && this.selectedItem?.sku === item.sku;
  }
}
