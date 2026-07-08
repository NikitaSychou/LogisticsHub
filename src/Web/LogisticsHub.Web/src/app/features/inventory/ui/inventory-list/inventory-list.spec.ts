import { ComponentFixture, TestBed } from '@angular/core/testing';
import { InventoryItemRow } from '../../models/inventory.models';
import { InventoryList } from './inventory-list';

describe('InventoryList', () => {
  let fixture: ComponentFixture<InventoryList>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [InventoryList],
    }).compileComponents();

    fixture = TestBed.createComponent(InventoryList);
  });

  it('emits the selected inventory item from the row action button', () => {
    const item: InventoryItemRow = {
      sku: 'SKU-1',
      name: 'Widget',
      quantityAvailable: 12,
    };
    let selectedItem: InventoryItemRow | undefined;
    fixture.componentRef.setInput('items', [item]);
    fixture.componentRef.setInput('selectedItem', null);
    fixture.componentRef.setInput('hasLoadedItems', true);
    fixture.componentRef.setInput('apiLoading', false);
    fixture.componentRef.setInput('apiError', '');
    fixture.componentInstance.itemSelected.subscribe((selected) => {
      selectedItem = selected;
    });

    fixture.detectChanges();

    const button = selectButton(fixture);
    expect(button.textContent).toContain('SKU-1');
    expect(button.getAttribute('aria-label')).toBe('Select inventory item SKU-1');

    button.click();

    expect(selectedItem).toBe(item);
  });
});

function selectButton(fixture: ComponentFixture<InventoryList>): HTMLButtonElement {
  const button = (fixture.nativeElement as HTMLElement).querySelector<HTMLButtonElement>('.inventory-select-button');
  if (!button) {
    throw new Error('Expected inventory selection button to render.');
  }

  return button;
}
