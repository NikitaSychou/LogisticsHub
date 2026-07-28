import { ComponentFixture, TestBed } from '@angular/core/testing';
import { StockAdjustmentForm } from './stock-adjustment-form';

describe('StockAdjustmentForm', () => {
  let fixture: ComponentFixture<StockAdjustmentForm>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [StockAdjustmentForm],
    }).compileComponents();

    fixture = TestBed.createComponent(StockAdjustmentForm);
    fixture.componentRef.setInput('adjusting', false);
    fixture.componentRef.setInput('error', '');
    fixture.componentRef.setInput('resetKey', 0);
    fixture.detectChanges();
  });

  it('submits a valid stock adjustment through Angular form handling and prevents native GET navigation', () => {
    const submitted: unknown[] = [];
    fixture.componentInstance.submitForm.subscribe((request) => submitted.push(request));

    setInputValue(fixture, '#stockAdjustmentQuantity', '3');

    const event = submitForm(fixture);

    expect(event.defaultPrevented).toBe(true);
    expect(submitted).toEqual([
      {
        quantity: 3,
      },
    ]);
  });
});

function setInputValue(fixture: ComponentFixture<StockAdjustmentForm>, selector: string, value: string): void {
  const input = query<HTMLInputElement>(fixture, selector);
  input.value = value;
  input.dispatchEvent(new Event('input'));
  fixture.detectChanges();
}

function submitForm(fixture: ComponentFixture<StockAdjustmentForm>): SubmitEvent {
  const event = new SubmitEvent('submit', { bubbles: true, cancelable: true });
  getForm(fixture).dispatchEvent(event);
  fixture.detectChanges();
  return event;
}

function getForm(fixture: ComponentFixture<StockAdjustmentForm>): HTMLFormElement {
  return query<HTMLFormElement>(fixture, 'form');
}

function query<TElement extends Element>(
  fixture: ComponentFixture<StockAdjustmentForm>,
  selector: string
): TElement {
  const element = (fixture.nativeElement as HTMLElement).querySelector<TElement>(selector);

  if (!element) {
    throw new Error(`Expected '${selector}' to render.`);
  }

  return element;
}