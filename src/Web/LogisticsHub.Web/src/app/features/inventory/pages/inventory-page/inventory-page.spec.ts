import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ApiAuthContext } from '../../../../core/http/api-auth-context';
import { InventoryApiService } from '../../data-access/inventory-api.service';
import {
  CreateInventoryItemRequest,
  CreateStockAdjustmentRequest,
  InventoryItemRow,
  InventoryItemsPageResult,
} from '../../models/inventory.models';
import { InventoryPage } from './inventory-page';

describe('InventoryPage item creation', () => {
  let fixture: ComponentFixture<InventoryPage>;
  let api: FakeInventoryApiService;

  beforeEach(async () => {
    api = new FakeInventoryApiService();

    await TestBed.configureTestingModule({
      imports: [InventoryPage],
      providers: [
        { provide: InventoryApiService, useValue: api },
        { provide: ApiAuthContext, useValue: { account: () => ({ username: 'review-user' }) } },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(InventoryPage);
    fixture.detectChanges();
  });

  it('calls the create inventory item API and refreshes the inventory list after successful form submission', async () => {
    const request: CreateInventoryItemRequest = {
      sku: 'SKU-001',
      name: 'Widget',
      quantityAvailable: 2,
    };

    api.resetCalls();

    openCreateItemForm(fixture);
    setInputValue(fixture, '#inventorySku', request.sku);
    setInputValue(fixture, '#inventoryName', request.name);
    setInputValue(fixture, '#inventoryQuantityAvailable', String(request.quantityAvailable));

    const event = submitCreateItemForm(fixture);
    await fixture.whenStable();
    fixture.detectChanges();

    expect(event.defaultPrevented).toBe(true);
    expect(api.createInventoryItemRequests).toEqual([request]);
    expect(api.requestedPages).toEqual([1]);
  });
});

function openCreateItemForm(fixture: ComponentFixture<InventoryPage>): void {
  const newItemButton = getButtonByText(fixture, 'New item');
  newItemButton.click();
  fixture.detectChanges();
}

function setInputValue(fixture: ComponentFixture<InventoryPage>, selector: string, value: string): void {
  const input = query<HTMLInputElement>(fixture, selector);
  input.value = value;
  input.dispatchEvent(new Event('input'));
  fixture.detectChanges();
}

function submitCreateItemForm(fixture: ComponentFixture<InventoryPage>): SubmitEvent {
  const event = new SubmitEvent('submit', { bubbles: true, cancelable: true });
  query<HTMLFormElement>(fixture, 'app-inventory-create-form form').dispatchEvent(event);
  fixture.detectChanges();
  return event;
}

function getButtonByText(fixture: ComponentFixture<InventoryPage>, text: string): HTMLButtonElement {
  const buttons = Array.from((fixture.nativeElement as HTMLElement).querySelectorAll('button'));
  const button = buttons.find((candidate) => candidate.textContent?.trim() === text);

  if (!button) {
    throw new Error(`Expected '${text}' button to render.`);
  }

  return button;
}

function query<TElement extends Element>(fixture: ComponentFixture<InventoryPage>, selector: string): TElement {
  const element = (fixture.nativeElement as HTMLElement).querySelector<TElement>(selector);

  if (!element) {
    throw new Error(`Expected '${selector}' to render.`);
  }

  return element;
}

class FakeInventoryApiService {
  readonly createInventoryItemRequests: CreateInventoryItemRequest[] = [];
  readonly requestedPages: number[] = [];

  resetCalls(): void {
    this.createInventoryItemRequests.length = 0;
    this.requestedPages.length = 0;
  }

  async getInventoryItemsPage(pageNumber: number): Promise<InventoryItemsPageResult> {
    this.requestedPages.push(pageNumber);

    return {
      items: [createdInventoryItem],
      pageNumber,
      pageSize: 20,
      hasMore: false,
    };
  }

  async createInventoryItem(request: CreateInventoryItemRequest): Promise<InventoryItemRow> {
    this.createInventoryItemRequests.push(request);
    return createdInventoryItem;
  }

  async createStockAdjustment(_sku: string, request: CreateStockAdjustmentRequest): Promise<InventoryItemRow> {
    return {
      ...createdInventoryItem,
      quantityAvailable: createdInventoryItem.quantityAvailable + request.quantity,
    };
  }
}

const createdInventoryItem: Required<InventoryItemRow> = {
  sku: 'SKU-001',
  name: 'Widget',
  quantityAvailable: 2,
};