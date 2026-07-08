import { CommonModule } from '@angular/common';
import {
  AfterViewInit,
  Component,
  ElementRef,
  OnDestroy,
  ViewChild,
  computed,
  inject,
  signal,
} from '@angular/core';
import { ApiAuthContext } from '../../../../core/http/api-auth-context';
import { formatProblemError } from '../../../../core/http/problem-error.mapper';
import { ErrorAlert } from '../../../../shared/ui/error-alert/error-alert';
import { LoadMoreState } from '../../../../shared/ui/load-more-state/load-more-state';
import { InventoryApiService } from '../../data-access/inventory-api.service';
import {
  CreateInventoryItemRequest,
  CreateStockAdjustmentRequest,
  InventoryItemRow,
} from '../../models/inventory.models';
import { InventoryCreateForm } from '../../ui/inventory-create-form/inventory-create-form';
import { InventoryDetails } from '../../ui/inventory-details/inventory-details';
import { InventoryList } from '../../ui/inventory-list/inventory-list';

@Component({
  selector: 'app-inventory-page',
  imports: [CommonModule, InventoryCreateForm, InventoryDetails, InventoryList, ErrorAlert, LoadMoreState],
  templateUrl: './inventory-page.html',
  styleUrl: './inventory-page.css',
})
export class InventoryPage implements AfterViewInit, OnDestroy {
  private readonly inventoryApi = inject(InventoryApiService);
  private readonly apiAuthContext = inject(ApiAuthContext);
  private observer?: IntersectionObserver;
  private sentinelElement?: HTMLElement;
  private viewReady = false;

  @ViewChild('inventoryScrollSentinel')
  private set inventoryScrollSentinel(value: ElementRef<HTMLElement> | undefined) {
    this.sentinelElement = value?.nativeElement;
    this.initializeInventoryObserver();
  }

  protected readonly apiLoading = signal(false);
  protected readonly loadingMore = signal(false);
  protected readonly apiError = signal('');
  protected readonly inventoryItems = signal<InventoryItemRow[]>([]);
  protected readonly selectedItem = signal<InventoryItemRow | null>(null);
  protected readonly showCreateItemForm = signal(false);
  protected readonly creatingItem = signal(false);
  protected readonly createItemError = signal('');
  protected readonly showStockAdjustmentForm = signal(false);
  protected readonly adjustingStock = signal(false);
  protected readonly stockAdjustmentError = signal('');
  protected readonly createItemResetKey = signal(0);
  protected readonly stockAdjustmentResetKey = signal(0);
  protected readonly hasLoadedItems = signal(false);
  protected readonly currentItemsPage = signal(0);
  protected readonly itemsPageSize = signal(0);
  protected readonly hasMoreItems = signal(false);
  protected readonly isInventoryLoading = computed(() => this.apiLoading() || this.loadingMore());

  ngAfterViewInit(): void {
    this.viewReady = true;
    this.initializeInventoryObserver();
    void this.ensureInventoryLoaded();
  }

  ngOnDestroy(): void {
    this.observer?.disconnect();
  }

  protected async loadInventory(): Promise<void> {
    await this.loadInventoryPage(1, { reset: true });
  }

  protected async loadMoreInventory(): Promise<void> {
    if (!this.hasMoreItems()) {
      return;
    }

    await this.loadInventoryPage(this.currentItemsPage() + 1, { reset: false });
  }

  protected selectItem(item: InventoryItemRow): void {
    this.selectedItem.set(item);
    this.stockAdjustmentError.set('');
    this.resetStockAdjustmentForm();
  }

  protected toggleCreateItemForm(): void {
    this.showCreateItemForm.update((value) => !value);
    this.createItemError.set('');
  }

  protected cancelCreateItem(): void {
    this.showCreateItemForm.set(false);
    this.createItemError.set('');
    this.resetCreateItemForm();
  }

  protected async submitCreateItem(request: CreateInventoryItemRequest): Promise<void> {
    if (this.creatingItem()) {
      return;
    }

    this.creatingItem.set(true);
    this.createItemError.set('');

    try {
      const createdItem = await this.inventoryApi.createInventoryItem(request);

      this.showCreateItemForm.set(false);
      this.resetCreateItemForm();
      await this.loadInventoryPage(1, { reset: true });

      if (createdItem?.sku) {
        const listItem = this.inventoryItems().find((item) => item.sku === createdItem.sku) ?? createdItem;
        this.selectItem(listItem);
      }
    } catch (error) {
      this.createItemError.set(this.formatError(error, 'Create inventory item failed.'));
    } finally {
      this.creatingItem.set(false);
    }
  }

  protected toggleStockAdjustmentForm(): void {
    this.showStockAdjustmentForm.update((value) => !value);
    this.stockAdjustmentError.set('');
  }

  protected cancelStockAdjustment(): void {
    this.showStockAdjustmentForm.set(false);
    this.stockAdjustmentError.set('');
    this.resetStockAdjustmentForm();
  }

  protected async submitStockAdjustment(request: CreateStockAdjustmentRequest): Promise<void> {
    if (this.adjustingStock()) {
      return;
    }

    const item = this.selectedItem();
    if (!item?.sku) {
      this.stockAdjustmentError.set('Select an inventory item before increasing stock.');
      return;
    }

    this.adjustingStock.set(true);
    this.stockAdjustmentError.set('');

    try {
      const adjustedItem = await this.inventoryApi.createStockAdjustment(item.sku, request);

      this.showStockAdjustmentForm.set(false);
      this.resetStockAdjustmentForm();
      await this.loadInventoryPage(1, { reset: true });

      const skuToSelect = adjustedItem?.sku ?? item.sku;
      const listItem = this.inventoryItems().find((inventoryItem) => inventoryItem.sku === skuToSelect) ?? adjustedItem;
      if (listItem) {
        this.selectItem(listItem);
      }
    } catch (error) {
      this.stockAdjustmentError.set(this.formatError(error, 'Stock adjustment failed.'));
    } finally {
      this.adjustingStock.set(false);
    }
  }

  private async loadInventoryPage(pageNumber: number, options: { reset: boolean }): Promise<void> {
    if (this.isInventoryLoading()) {
      return;
    }

    if (!this.apiAuthContext.account()) {
      this.apiError.set('Sign in before calling the Gateway.');
      return;
    }

    this.apiLoading.set(options.reset);
    this.loadingMore.set(!options.reset);
    this.apiError.set('');

    if (options.reset) {
      this.resetInventoryPageState();
    }

    try {
      const page = await this.inventoryApi.getInventoryItemsPage(pageNumber);
      this.applyInventoryPage(page.items, {
        pageNumber: page.pageNumber,
        pageSize: page.pageSize,
        hasMore: page.hasMore,
        reset: options.reset,
      });
      this.hasLoadedItems.set(true);
    } catch (error) {
      this.apiError.set(error instanceof Error ? error.message : 'Inventory load failed.');
    } finally {
      this.apiLoading.set(false);
      this.loadingMore.set(false);
    }
  }

  private async ensureInventoryLoaded(): Promise<void> {
    if (
      !this.apiAuthContext.account() ||
      this.hasLoadedItems() ||
      this.inventoryItems().length > 0 ||
      this.isInventoryLoading()
    ) {
      return;
    }

    await this.loadInventoryPage(1, { reset: true });
  }

  private resetCreateItemForm(): void {
    this.createItemResetKey.update((value) => value + 1);
  }

  private resetStockAdjustmentForm(): void {
    this.stockAdjustmentResetKey.update((value) => value + 1);
  }

  private formatError(error: unknown, fallback: string): string {
    return formatProblemError(error, fallback);
  }

  private resetInventoryPageState(): void {
    this.inventoryItems.set([]);
    this.selectedItem.set(null);
    this.hasLoadedItems.set(false);
    this.currentItemsPage.set(0);
    this.itemsPageSize.set(0);
    this.hasMoreItems.set(false);
  }

  private applyInventoryPage(
    items: InventoryItemRow[],
    page: { pageNumber: number; pageSize: number; hasMore: boolean; reset: boolean }
  ): void {
    this.inventoryItems.set(page.reset ? items : [...this.inventoryItems(), ...items]);
    this.currentItemsPage.set(page.pageNumber);
    this.itemsPageSize.set(page.pageSize);
    this.hasMoreItems.set(page.hasMore);
  }

  private initializeInventoryObserver(): void {
    if (!this.viewReady || !this.sentinelElement || !('IntersectionObserver' in window)) {
      return;
    }

    this.observer?.disconnect();
    this.observer = new IntersectionObserver(
      (entries) => {
        const isVisible = entries.some((entry) => entry.isIntersecting);
        if (isVisible && this.hasLoadedItems() && this.hasMoreItems()) {
          void this.loadMoreInventory();
        }
      },
      { rootMargin: '240px 0px' }
    );
    this.observer.observe(this.sentinelElement);
  }
}
