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
    getCardInvoicePayment: () => of({ isPaid: false, amount: 0, paidAt: null }),
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

  it('carries the benefit balance across the year boundary', () => {
    const fixture = TestBed.createComponent(DashboardComponent);
    const component = fixture.componentInstance;
    component.year = 2026;
    component.month = 1;
    component.incomes = [
      {
        id: 1,
        description: 'Crédito Flash anterior',
        accountName: 'Flash',
        categoryName: 'Benefício',
        receivedAt: '2025-12-10',
        amount: 300,
        type: 1,
        investmentWithdrawalId: null,
      },
      {
        id: 2,
        description: 'Crédito Flash atual',
        accountName: 'Flash',
        categoryName: 'Benefício',
        receivedAt: '2026-01-05',
        amount: 200,
        type: 1,
        investmentWithdrawalId: null,
      },
    ];
    component.benefitEntries = [
      { id: 1, description: 'Recarga antiga', entryDate: '2025-12-11', amount: 100, type: 0 },
      { id: 2, description: 'Compra antiga', entryDate: '2025-12-12', amount: 25, type: 1 },
      { id: 3, description: 'Compra atual', entryDate: '2026-01-06', amount: 10, type: 1 },
    ];

    expect(component.previousMonthLabel).toBe('Dezembro 2025');
    expect(component.previousBenefitBalance).toBe(375);
    expect(component.benefitBalance).toBe(565);
  });

  it('totals the full selected-month expenses', () => {
    const fixture = TestBed.createComponent(DashboardComponent);
    const component = fixture.componentInstance;
    component.year = 2026;
    component.month = 11;
    component.expenses = [
      {
        id: 1,
        description: 'Conta paga',
        categoryId: 1,
        expenseDate: '2026-11-02',
        amount: 125.5,
        isPaid: true,
        recurrenceType: 0,
        installmentNumber: null,
        totalInstallments: null,
        isInvestmentAdjustment: false,
        isInvoicePayment: false,
      },
      {
        id: 2,
        description: 'Conta pendente',
        categoryId: 1,
        expenseDate: '2026-11-15',
        amount: 75.25,
        isPaid: false,
        recurrenceType: 0,
        installmentNumber: null,
        totalInstallments: null,
        isInvestmentAdjustment: false,
        isInvoicePayment: false,
      },
      {
        id: 3,
        description: 'Outro mês',
        categoryId: 1,
        expenseDate: '2026-12-01',
        amount: 999,
        isPaid: false,
        recurrenceType: 0,
        installmentNumber: null,
        totalInstallments: null,
        isInvestmentAdjustment: false,
        isInvoicePayment: false,
      },
    ];

    expect(component.monthExpenseTotal).toBe(200.75);
  });

  it('shows expense total without the overview metric cards', async () => {
    const fixture = TestBed.createComponent(DashboardComponent);
    fixture.detectChanges();
    await fixture.whenStable();
    const component = fixture.componentInstance;
    component.area = 'expenses';
    component.expenses = [
      {
        id: 1,
        description: 'Conta',
        categoryId: 1,
        expenseDate: `${component.year}-${String(component.month).padStart(2, '0')}-02`,
        amount: 125,
        isPaid: true,
        recurrenceType: 0,
        installmentNumber: null,
        totalInstallments: null,
        isInvestmentAdjustment: false,
        isInvoicePayment: false,
      },
    ];
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelector('.metric-grid')).toBeNull();
    expect(fixture.nativeElement.textContent).toContain(`Total das despesas em ${component.monthLabel}`);
  });
});
