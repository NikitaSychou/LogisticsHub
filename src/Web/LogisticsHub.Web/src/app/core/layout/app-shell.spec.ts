import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { AppShell } from './app-shell';

describe('AppShell', () => {
  let fixture: ComponentFixture<AppShell>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [AppShell],
      providers: [provideRouter([])],
    }).compileComponents();

    fixture = TestBed.createComponent(AppShell);
  });

  it('renders the signed-out login action', () => {
    fixture.componentRef.setInput('loading', false);
    fixture.componentRef.setInput('isSignedIn', false);
    fixture.componentRef.setInput('signedInName', '');
    fixture.componentRef.setInput('navigationItems', []);

    fixture.detectChanges();

    const element = fixture.nativeElement as HTMLElement;
    expect(element.textContent).toContain('Sign in with Microsoft');
  });

  it('renders signed-in navigation items', () => {
    fixture.componentRef.setInput('loading', false);
    fixture.componentRef.setInput('isSignedIn', true);
    fixture.componentRef.setInput('signedInName', 'Operations User');
    fixture.componentRef.setInput('navigationItems', [{ label: 'Operations', path: '/operations' }]);

    fixture.detectChanges();

    const element = fixture.nativeElement as HTMLElement;
    expect(element.textContent).toContain('Operations User');
    expect(element.textContent).toContain('Operations');
    expect(element.textContent).toContain('Sign out');
  });
});
