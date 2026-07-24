import { ComponentFixture, TestBed } from '@angular/core/testing';
import { CompanyCreateForm } from './company-create-form';

describe('CompanyCreateForm', () => {
  let fixture: ComponentFixture<CompanyCreateForm>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [CompanyCreateForm],
    }).compileComponents();

    fixture = TestBed.createComponent(CompanyCreateForm);
    fixture.componentRef.setInput('creating', false);
    fixture.componentRef.setInput('error', '');
    fixture.componentRef.setInput('resetKey', 0);
    fixture.detectChanges();
  });

  it('submits a valid company through Angular form handling', () => {
    const submitted: unknown[] = [];
    fixture.componentInstance.submitForm.subscribe((request) => submitted.push(request));

    setInputValue(fixture, '#companyName', 'Acme Logistics');
    setInputValue(fixture, '#companyExternalCode', ' ACM-001 ');
    setSelectValue(fixture, '#companyStatus', 'Inactive');

    submitForm(fixture);

    expect(submitted).toEqual([
      {
        name: 'Acme Logistics',
        externalCode: 'ACM-001',
        status: 'Inactive',
      },
    ]);
  });

  it('prevents native GET navigation when the form is submitted', () => {
    setInputValue(fixture, '#companyName', 'Acme Logistics');

    const form = getForm(fixture);
    const event = new SubmitEvent('submit', { bubbles: true, cancelable: true });

    form.dispatchEvent(event);
    fixture.detectChanges();

    expect(event.defaultPrevented).toBe(true);
  });

  it('does not submit an invalid company form', () => {
    const submitted: unknown[] = [];
    fixture.componentInstance.submitForm.subscribe((request) => submitted.push(request));

    submitForm(fixture);

    expect(submitted).toEqual([]);
  });
});

function setInputValue(fixture: ComponentFixture<CompanyCreateForm>, selector: string, value: string): void {
  const input = query<HTMLInputElement>(fixture, selector);
  input.value = value;
  input.dispatchEvent(new Event('input'));
  fixture.detectChanges();
}

function setSelectValue(fixture: ComponentFixture<CompanyCreateForm>, selector: string, value: string): void {
  const select = query<HTMLSelectElement>(fixture, selector);
  select.value = value;
  select.dispatchEvent(new Event('change'));
  fixture.detectChanges();
}

function submitForm(fixture: ComponentFixture<CompanyCreateForm>): void {
  getForm(fixture).dispatchEvent(new SubmitEvent('submit', { bubbles: true, cancelable: true }));
  fixture.detectChanges();
}

function getForm(fixture: ComponentFixture<CompanyCreateForm>): HTMLFormElement {
  return query<HTMLFormElement>(fixture, 'form');
}

function query<TElement extends Element>(fixture: ComponentFixture<CompanyCreateForm>, selector: string): TElement {
  const element = (fixture.nativeElement as HTMLElement).querySelector<TElement>(selector);

  if (!element) {
    throw new Error(`Expected '' to render.`);
  }

  return element;
}