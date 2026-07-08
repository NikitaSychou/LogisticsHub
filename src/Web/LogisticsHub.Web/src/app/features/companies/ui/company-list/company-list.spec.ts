import { ComponentFixture, TestBed } from '@angular/core/testing';
import { CompanyRow } from '../../models/company.models';
import { CompanyList } from './company-list';

describe('CompanyList', () => {
  let fixture: ComponentFixture<CompanyList>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [CompanyList],
    }).compileComponents();

    fixture = TestBed.createComponent(CompanyList);
  });

  it('emits the selected company from the row action button', () => {
    const company: CompanyRow = {
      id: 'company-1',
      externalCode: 'C-001',
      name: 'Acme Logistics',
      status: 'Active',
    };
    let selectedCompany: CompanyRow | undefined;
    fixture.componentRef.setInput('companies', [company]);
    fixture.componentRef.setInput('selectedCompany', null);
    fixture.componentRef.setInput('hasLoadedCompanies', true);
    fixture.componentRef.setInput('apiLoading', false);
    fixture.componentRef.setInput('apiError', '');
    fixture.componentInstance.companySelected.subscribe((selected) => {
      selectedCompany = selected;
    });

    fixture.detectChanges();

    const button = selectButton(fixture);
    expect(button.textContent).toContain('Acme Logistics');
    expect(button.getAttribute('aria-label')).toBe('Select company Acme Logistics');

    button.click();

    expect(selectedCompany).toBe(company);
  });
});

function selectButton(fixture: ComponentFixture<CompanyList>): HTMLButtonElement {
  const button = (fixture.nativeElement as HTMLElement).querySelector<HTMLButtonElement>('.company-select-button');
  if (!button) {
    throw new Error('Expected company selection button to render.');
  }

  return button;
}
