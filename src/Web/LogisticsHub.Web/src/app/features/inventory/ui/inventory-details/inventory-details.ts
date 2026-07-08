import { CommonModule } from '@angular/common';
import { Component, EventEmitter, Input, Output } from '@angular/core';
import { CreateStockAdjustmentRequest, InventoryItemRow } from '../../models/inventory.models';
import { StockAdjustmentForm } from '../stock-adjustment-form/stock-adjustment-form';

@Component({
  selector: 'app-inventory-details',
  imports: [CommonModule, StockAdjustmentForm],
  templateUrl: './inventory-details.html',
  styleUrl: './inventory-details.css',
})
export class InventoryDetails {
  @Input({ required: true }) item!: InventoryItemRow | null;
  @Input({ required: true }) showStockAdjustmentForm = false;
  @Input({ required: true }) adjustingStock = false;
  @Input({ required: true }) stockAdjustmentError = '';
  @Input({ required: true }) stockAdjustmentResetKey = 0;

  @Output() toggleStockAdjustment = new EventEmitter<void>();
  @Output() submitStockAdjustment = new EventEmitter<CreateStockAdjustmentRequest>();
  @Output() cancelStockAdjustment = new EventEmitter<void>();
}
