import { CommonModule } from '@angular/common';
import {
  AfterViewInit,
  Component,
  ElementRef,
  Input,
  OnChanges,
  OnDestroy,
  SimpleChanges,
  ViewChild,
  computed,
  inject,
  signal,
} from '@angular/core';
import { AccountInfo } from '@azure/msal-browser';
import { InventoryApiService } from './inventory-api.service';
import { InventoryItemRow, PagedResponse } from './inventory.models';

@Component({
  selector: 'app-inventory-page',
  imports: [CommonModule],
  templateUrl: './inventory-page.html',
  styleUrl: './inventory-page.css',
})
export class InventoryPage implements AfterViewInit, OnChanges, OnDestroy {
  private readonly inventoryApi = inject(InventoryApiService);
  private observer?: IntersectionObserver;
  private sentinelElement?: HTMLElement;
  private viewReady = false;

  @Input({ required: true }) accessTokenFactory!: () => Promise<string>;
  @Input({ required: true }) account!: AccountInfo | null;
  @Input({ required: true }) active = false;

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

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['active'] || changes['account']) {
      void this.ensureInventoryLoaded();
    }
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
  }

  protected isSelectedItem(item: InventoryItemRow): boolean {
    return item.sku !== undefined && this.selectedItem()?.sku === item.sku;
  }

  private async loadInventoryPage(pageNumber: number, options: { reset: boolean }): Promise<void> {
    if (this.isInventoryLoading()) {
      return;
    }

    if (!this.account) {
      this.apiError.set('Sign in before calling the Gateway.');
      return;
    }

    this.apiLoading.set(options.reset);
    this.loadingMore.set(!options.reset);
    this.apiError.set('');

    if (options.reset) {
      this.inventoryItems.set([]);
      this.selectedItem.set(null);
      this.hasLoadedItems.set(false);
      this.currentItemsPage.set(0);
      this.itemsPageSize.set(0);
      this.hasMoreItems.set(false);
    }

    try {
      const body = await this.inventoryApi.getInventoryItemsPage(pageNumber, await this.accessTokenFactory());
      const page = this.toPagedInventoryItems(this.parseBody(body), pageNumber);
      this.inventoryItems.set(options.reset ? page.items : [...this.inventoryItems(), ...page.items]);
      this.currentItemsPage.set(page.pageNumber);
      this.itemsPageSize.set(page.pageSize);
      this.hasMoreItems.set(page.hasMore);
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
      !this.active ||
      !this.account ||
      this.hasLoadedItems() ||
      this.inventoryItems().length > 0 ||
      this.isInventoryLoading()
    ) {
      return;
    }

    await this.loadInventoryPage(1, { reset: true });
  }

  private parseBody(body: string): unknown {
    if (!body) {
      return null;
    }

    try {
      return JSON.parse(body);
    } catch {
      return body;
    }
  }

  private extractInventoryItems(payload: unknown[]): InventoryItemRow[] {
    return payload.map((item) => {
      const record = this.asRecord(item);

      return {
        sku: this.stringValue(record, ['sku']),
        name: this.stringValue(record, ['name']),
        quantityAvailable: this.numberValue(record, 'quantityAvailable'),
        raw: item,
      };
    });
  }

  private toPagedInventoryItems(payload: unknown, requestedPage: number): PagedResponse<InventoryItemRow> {
    const record = this.asRecord(payload);
    const rawItems = Array.isArray(record['items']) ? record['items'] : [];

    return {
      items: this.extractInventoryItems(rawItems),
      pageNumber: this.numberValue(record, 'pageNumber') ?? requestedPage,
      pageSize: this.numberValue(record, 'pageSize') ?? rawItems.length,
      hasMore: this.booleanValue(record, 'hasMore') ?? false,
    };
  }

  private asRecord(value: unknown): Record<string, unknown> {
    return value !== null && typeof value === 'object' ? (value as Record<string, unknown>) : {};
  }

  private stringValue(record: Record<string, unknown>, keys: string[]): string | undefined {
    for (const key of keys) {
      const value = record[key];
      if (typeof value === 'string' && value.trim().length > 0) {
        return value;
      }
    }

    return undefined;
  }

  private numberValue(record: Record<string, unknown>, key: string): number | undefined {
    const value = record[key];
    return typeof value === 'number' && Number.isFinite(value) ? value : undefined;
  }

  private booleanValue(record: Record<string, unknown>, key: string): boolean | undefined {
    const value = record[key];
    return typeof value === 'boolean' ? value : undefined;
  }

  private initializeInventoryObserver(): void {
    if (!this.viewReady || !this.sentinelElement || !('IntersectionObserver' in window)) {
      return;
    }

    this.observer?.disconnect();
    this.observer = new IntersectionObserver(
      (entries) => {
        const isVisible = entries.some((entry) => entry.isIntersecting);
        if (isVisible && this.active && this.hasLoadedItems() && this.hasMoreItems()) {
          void this.loadMoreInventory();
        }
      },
      { rootMargin: '240px 0px' }
    );
    this.observer.observe(this.sentinelElement);
  }
}
