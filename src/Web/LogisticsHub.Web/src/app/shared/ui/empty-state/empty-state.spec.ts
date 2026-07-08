import { ComponentFixture, TestBed } from '@angular/core/testing';
import { EmptyState } from './empty-state';

describe('EmptyState', () => {
  let fixture: ComponentFixture<EmptyState>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [EmptyState],
    }).compileComponents();

    fixture = TestBed.createComponent(EmptyState);
  });

  it('renders a title and message when both are provided', () => {
    fixture.componentRef.setInput('title', 'Nothing here');
    fixture.componentRef.setInput('message', 'Create an item to get started.');

    fixture.detectChanges();

    const element = fixture.nativeElement as HTMLElement;
    expect(element.querySelector('h2')?.textContent).toContain('Nothing here');
    expect(element.textContent).toContain('Create an item to get started.');
  });

  it('renders the message without a title', () => {
    fixture.componentRef.setInput('message', 'No details selected.');

    fixture.detectChanges();

    const element = fixture.nativeElement as HTMLElement;
    expect(element.querySelector('h2')).toBeNull();
    expect(element.textContent).toContain('No details selected.');
  });
});
