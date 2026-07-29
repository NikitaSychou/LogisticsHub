import { ComponentFixture, TestBed } from '@angular/core/testing';
import { InventoryCreateForm } from './inventory-create-form';

describe('InventoryCreateForm', () => {
  let fixture: ComponentFixture<InventoryCreateForm>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [InventoryCreateForm],
    }).compileComponents();

    fixture = TestBed.createComponent(InventoryCreateForm);
    fixture.componentRef.setInput('creating', false);
    fixture.componentRef.setInput('error', '');
    fixture.componentRef.setInput('resetKey', 0);
    fixture.detectChanges();
  });

  it('submits a valid inventory item through Angular form handling and prevents native GET navigation', () => {
    const submitted: unknown[] = [];
    fixture.componentInstance.submitForm.subscribe((request) => submitted.push(request));

    setInputValue(fixture, '#inventorySku', ' SKU-001 ');
    setInputValue(fixture, '#inventoryName', 'Widget');
    setInputValue(fixture, '#inventoryQuantityAvailable', '2');

    const event = submitForm(fixture);

    expect(event.defaultPrevented).toBe(true);
    expect(submitted).toEqual([
      {
        sku: 'SKU-001',
        name: 'Widget',
        quantityAvailable: 2,
      },
    ]);
  });
});

function setInputValue(fixture: ComponentFixture<InventoryCreateForm>, selector: string, value: string): void {
  const input = query<HTMLInputElement>(fixture, selector);
  input.value = value;
  input.dispatchEvent(new Event('input'));
  fixture.detectChanges();
}

function submitForm(fixture: ComponentFixture<InventoryCreateForm>): SubmitEvent {
  const event = new SubmitEvent('submit', { bubbles: true, cancelable: true });
  getForm(fixture).dispatchEvent(event);
  fixture.detectChanges();
  return event;
}

function getForm(fixture: ComponentFixture<InventoryCreateForm>): HTMLFormElement {
  return query<HTMLFormElement>(fixture, 'form');
}

function query<TElement extends Element>(
  fixture: ComponentFixture<InventoryCreateForm>,
  selector: string
): TElement {
  const element = (fixture.nativeElement as HTMLElement).querySelector<TElement>(selector);

  if (!element) {
    throw new Error(`Expected '${selector}' to render.`);
  }

  return element;
}