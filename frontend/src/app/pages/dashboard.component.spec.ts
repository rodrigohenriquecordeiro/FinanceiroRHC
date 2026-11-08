import { provideRouter } from '@angular/router';
import { TestBed } from '@angular/core/testing';
import { of } from 'rxjs';
import { ApiService } from '../core/api.service';
import { DashboardComponent } from './dashboard.component';

describe('DashboardComponent', () => {
  const api = {
    getIncomes: () => of([]),
    getExpenses: () => of([]),
    getCategories: () => of([]),
    getCards: () => of([]),
    getCardPurchases: () => of([]),
    getBenefitEntries: () => of([]),
    getMonthlySummary: () => of({
      year: new Date().getFullYear(),
      month: new Date().getMonth() + 1,
      openingBalance: 0,
      incomeTotal: 0,
      expensesTotal: 0,
      paidExpensesTotal: 0,
      currentBalance: 0,
      projectedBalance: 0,
    }),
    getAnnualSummary: () => of({
      year: new Date().getFullYear(),
      incomeTotal: 0,
      expensesTotal: 0,
      creditCardTotal: 0,
      benefitRechargeTotal: 0,
      benefitPurchaseTotal: 0,
      months: [],
    }),
  };

  beforeEach(async () => {
    localStorage.setItem('financeiro-rhc-token', 'test-token');
    await TestBed.configureTestingModule({
      imports: [DashboardComponent],
      providers: [
        provideRouter([]),
        { provide: ApiService, useValue: api },
      ],
    }).compileComponents();
  });

  afterEach(() => {
    localStorage.removeItem('financeiro-rhc-token');
  });

  it('shows an empty-month message after successful requests with no entries', async () => {
    const fixture = TestBed.createComponent(DashboardComponent);
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();

    expect(fixture.componentInstance.loading).toBe(false);
    expect(fixture.nativeElement.textContent).toContain('Ainda não há lançamentos em');
    expect(fixture.nativeElement.textContent).not.toContain('Carregando suas informações');
  });

  it('totals only the credit-card purchases in the selected month', () => {
    const fixture = TestBed.createComponent(DashboardComponent);
    const component = fixture.componentInstance;
    component.year = 2026;
    component.month = 11;
    component.purchases = [
      {
        id: 1,
        creditCardId: 1,
        categoryId: 1,
        description: 'Compra do mês',
        purchaseDate: '2026-11-08',
        amount: 1234.56,
        recurrenceType: 0,
        installmentNumber: null,
        totalInstallments: null,
      },
      {
        id: 2,
        creditCardId: 1,
        categoryId: 1,
        description: 'Compra do próximo mês',
        purchaseDate: '2026-12-08',
        amount: 75.5,
        recurrenceType: 0,
        installmentNumber: null,
        totalInstallments: null,
      },
    ];

    expect(component.monthCardTotal).toBe(1234.56);
  });
});
