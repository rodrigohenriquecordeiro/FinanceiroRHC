import { CurrencyPipe, DatePipe, DecimalPipe } from '@angular/common';
import { ChangeDetectorRef, Component, OnInit, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { forkJoin, map, Observable, switchMap } from 'rxjs';
import { ApiService } from '../core/api.service';
import {
  AnnualSummary,
  BenefitEntry,
  BenefitEntryInput,
  CardPurchase,
  CardPurchaseInput,
  Category,
  CreditCard,
  Expense,
  ExpenseInput,
  Income,
  IncomeInput,
  MonthlySummary,
} from '../core/models';

type Area = 'overview' | 'income' | 'expenses' | 'card' | 'benefit' | 'annual';

@Component({
  selector: 'app-dashboard',
  imports: [CurrencyPipe, DatePipe, DecimalPipe, FormsModule, RouterLink],
  templateUrl: './dashboard.component.html',
})
export class DashboardComponent implements OnInit {
  private readonly api = inject(ApiService);
  private readonly router = inject(Router);
  private readonly changeDetector = inject(ChangeDetectorRef);

  readonly areas: { id: Area; label: string; icon: string }[] = [
    { id: 'overview', label: 'Visão geral', icon: '◫' },
    { id: 'income', label: 'Receitas', icon: '↗' },
    { id: 'expenses', label: 'Despesas', icon: '↘' },
    { id: 'card', label: 'Cartão de crédito', icon: '▤' },
    { id: 'benefit', label: 'Vale alimentação', icon: '✳' },
    { id: 'annual', label: 'Ano', icon: '▥' },
  ];
  readonly monthNames = ['Janeiro', 'Fevereiro', 'Março', 'Abril', 'Maio', 'Junho', 'Julho', 'Agosto', 'Setembro', 'Outubro', 'Novembro', 'Dezembro'];
  readonly today = new Date();
  area: Area = 'overview';
  month = this.today.getMonth() + 1;
  year = this.today.getFullYear();
  name = localStorage.getItem('financeiro-rhc-name') ?? 'Olá';
  incomes: Income[] = [];
  expenses: Expense[] = [];
  categories: Category[] = [];
  cards: CreditCard[] = [];
  purchases: CardPurchase[] = [];
  benefitEntries: BenefitEntry[] = [];
  summary: MonthlySummary | null = null;
  annual: AnnualSummary | null = null;
  loading = true;
  saving = false;
  error = '';
  notice = '';
  showCategoryEditor = false;
  showCardEditor = false;
  deleteDialog: {
    message: string;
    successMessage: string;
    request: Observable<unknown>;
    futureRequest?: Observable<unknown>;
    futureSuccessMessage?: string;
  } | null = null;
  editingIncomeId?: number;
  editingExpenseId?: number;
  editingRecurringExpense = false;
  editingPurchaseId?: number;
  editingRecurringPurchase = false;
  editingBenefitId?: number;
  editingCategoryId?: number;
  editingCardId?: number;
  categoryName = '';
  categoryType = 'expense';
  cardName = '';
  openingBalance = 0;

  incomeForm: IncomeInput = this.emptyIncome();
  expenseForm: ExpenseInput = this.emptyExpense();
  purchaseForm: CardPurchaseInput = this.emptyPurchase();
  benefitForm: BenefitEntryInput = this.emptyBenefit();

  ngOnInit() {
    if (!localStorage.getItem('financeiro-rhc-token')) {
      void this.router.navigateByUrl('/login');
      return;
    }
    this.load();
  }

  get areaLabel() {
    return this.areas.find((item) => item.id === this.area)?.label ?? 'Painel';
  }

  get benefitRechargeCount() {
    return this.monthBenefitEntries.filter((entry) => entry.type === 0).length;
  }

  get benefitPurchaseCount() {
    return this.monthBenefitEntries.filter((entry) => entry.type === 1).length;
  }

  get monthLabel() {
    return this.monthNames[this.month - 1];
  }

  get monthIncomes() {
    return this.incomes.filter((item) => this.inMonth(item.receivedAt));
  }

  get monthExpenses() {
    return this.expenses.filter((item) => this.inMonth(item.expenseDate));
  }

  get monthPurchases() {
    return this.purchases.filter((item) => this.inMonth(item.purchaseDate));
  }

  get monthCardTotal() {
    return this.monthPurchases.reduce((total, item) => total + item.amount, 0);
  }

  get monthBenefitEntries() {
    return this.benefitEntries.filter((item) => this.inMonth(item.entryDate));
  }

  get hasMonthEntries() {
    return this.monthIncomes.length > 0
      || this.monthExpenses.length > 0
      || this.monthPurchases.length > 0
      || this.monthBenefitEntries.length > 0;
  }

  get benefitBalance() {
    return this.benefitEntries
      .filter((entry) => {
        const date = new Date(`${entry.entryDate}T00:00:00`);
        return date.getFullYear() < this.year
          || (date.getFullYear() === this.year && date.getMonth() + 1 <= this.month);
      })
      .reduce((balance, entry) => balance + (entry.type === 0 ? entry.amount : -entry.amount), 0);
  }

  get budgetPercentage() {
    const total = this.summary?.incomeTotal ?? 0;
    return (amount: number) => total ? Math.min((amount / total) * 100, 100) : 0;
  }

  chooseArea(area: Area) {
    this.area = area;
    this.error = '';
    this.notice = '';
    if (area === 'annual') {
      this.loadAnnual();
    }
  }

  shiftMonth(delta: number) {
    const date = new Date(this.year, this.month - 1 + delta, 1);
    this.month = date.getMonth() + 1;
    this.year = date.getFullYear();
    this.load();
  }

  changeYear(delta: number) {
    this.year += delta;
    this.loadAnnual();
  }

  saveOpeningBalance() {
    if (!this.summary) return;
    this.saving = true;
    this.api.saveOpeningBalance(this.year, this.month, this.openingBalance).subscribe({
      next: (summary) => {
        this.summary = summary;
        this.openingBalance = summary.openingBalance;
        this.done('Saldo inicial atualizado.');
        this.changeDetector.markForCheck();
      },
      error: (error) => this.failed(error),
    });
  }

  openCategoryEditor() {
    this.startCategoryEdit();
    this.categoryType = 'expense';
    this.showCategoryEditor = true;
  }

  editIncome(item: Income) {
    this.editingIncomeId = item.id;
    this.incomeForm = { description: item.description, accountName: item.accountName, categoryName: item.categoryName, receivedAt: item.receivedAt, amount: item.amount };
    this.area = 'income';
    window.scrollTo({ top: 0, behavior: 'smooth' });
  }

  saveIncome() {
    this.save(this.api.saveIncome(this.incomeForm, this.editingIncomeId), () => {
      this.incomeForm = this.emptyIncome();
      this.editingIncomeId = undefined;
      this.load();
    }, 'Receita salva com sucesso.');
  }

  deleteIncome(item: Income) {
    this.askDelete(`Deseja excluir a receita “${item.description}”?`, this.api.deleteIncome(item.id), 'Receita excluída.');
  }

  editExpense(item: Expense) {
    this.editingExpenseId = item.id;
    this.editingRecurringExpense = item.recurrenceType !== 0;
    this.expenseForm = {
      description: item.description,
      categoryId: item.categoryId,
      expenseDate: item.expenseDate,
      amount: item.amount,
      isPaid: item.isPaid,
      recurrenceType: 0,
      startInstallment: item.installmentNumber ?? 1,
      totalInstallments: item.totalInstallments,
    };
    this.area = 'expenses';
    window.scrollTo({ top: 0, behavior: 'smooth' });
  }

  saveExpense() {
    this.save(this.api.saveExpense(this.expenseForm, this.editingExpenseId), () => {
      this.expenseForm = this.emptyExpense();
      this.editingExpenseId = undefined;
      this.editingRecurringExpense = false;
      this.load();
    }, 'Despesa salva com sucesso.');
  }

  deleteExpense(item: Expense) {
    const installment = item.installmentNumber
      ? ` (parcela ${item.installmentNumber}${item.totalInstallments ? ` de ${item.totalInstallments}` : ''})`
      : '';
    this.askDelete(
      `Deseja excluir somente esta despesa${installment}: “${item.description}”? As próximas parcelas recorrentes serão mantidas.`,
      this.api.deleteExpense(item.id),
      'Despesa excluída.',
      item.recurrenceType !== 0
        ? this.api.cancelExpenseRecurrenceFrom(item.id)
        : undefined,
      'Esta despesa e as parcelas futuras foram excluídas.',
    );
  }

  editPurchase(item: CardPurchase) {
    this.editingPurchaseId = item.id;
    this.editingRecurringPurchase = item.recurrenceType !== 0;
    this.purchaseForm = {
      creditCardId: item.creditCardId,
      categoryId: item.categoryId ?? this.categories.find((category) => category.type === 'expense')?.id ?? 0,
      description: item.description,
      purchaseDate: item.purchaseDate,
      amount: item.amount,
      recurrenceType: 0,
      startInstallment: item.installmentNumber ?? 1,
      totalInstallments: item.totalInstallments,
    };
    this.area = 'card';
    window.scrollTo({ top: 0, behavior: 'smooth' });
  }

  savePurchase() {
    this.save(this.api.saveCardPurchase(this.purchaseForm, this.editingPurchaseId), () => {
      this.purchaseForm = this.emptyPurchase();
      this.editingPurchaseId = undefined;
      this.editingRecurringPurchase = false;
      this.load();
    }, 'Compra salva com sucesso.');
  }

  deletePurchase(item: CardPurchase) {
    const installment = item.installmentNumber
      ? ` (parcela ${item.installmentNumber}${item.totalInstallments ? ` de ${item.totalInstallments}` : ''})`
      : '';
    this.askDelete(
      `Deseja excluir somente esta compra${installment}: “${item.description}”? As próximas compras recorrentes serão mantidas.`,
      this.api.deleteCardPurchase(item.id),
      'Compra excluída.',
      item.recurrenceType !== 0
        ? this.api.cancelCardPurchaseRecurrenceFrom(item.id)
        : undefined,
      'Esta compra e as futuras foram excluídas.',
    );
  }

  editBenefit(item: BenefitEntry) {
    this.editingBenefitId = item.id;
    this.benefitForm = { description: item.description, entryDate: item.entryDate, amount: item.amount, type: item.type };
    this.area = 'benefit';
    window.scrollTo({ top: 0, behavior: 'smooth' });
  }

  saveBenefit() {
    this.save(this.api.saveBenefitEntry(this.benefitForm, this.editingBenefitId), () => {
      this.benefitForm = this.emptyBenefit();
      this.editingBenefitId = undefined;
      this.load();
    }, 'Lançamento do benefício salvo.');
  }

  deleteBenefit(item: BenefitEntry) {
    this.askDelete(`Deseja excluir o lançamento “${item.description}”?`, this.api.deleteBenefitEntry(item.id), 'Lançamento excluído.');
  }

  startCategoryEdit(category?: Category) {
    this.editingCategoryId = category?.id;
    this.categoryName = category?.name ?? '';
    this.categoryType = category?.type ?? 'expense';
  }

  saveCategory() {
    this.save(this.api.saveCategory({ name: this.categoryName, type: this.categoryType }, this.editingCategoryId), () => {
      this.startCategoryEdit();
      this.load();
    }, 'Categoria salva.');
  }

  deleteCategory(category: Category) {
    this.askDelete(
      `Deseja excluir a categoria “${category.name}”? Categorias vinculadas a despesas não podem ser removidas.`,
      this.api.deleteCategory(category.id),
      'Categoria excluída.',
    );
  }

  startCardEdit(card?: CreditCard) {
    this.editingCardId = card?.id;
    this.cardName = card?.name ?? '';
  }

  saveCard() {
    this.save(this.api.saveCard({ name: this.cardName }, this.editingCardId), () => {
      this.startCardEdit();
      this.load();
    }, 'Cartão salvo.');
  }

  deleteCard(card: CreditCard) {
    this.askDelete(
      `Deseja excluir o cartão “${card.name}”? Cartões com compras registradas não podem ser removidos.`,
      this.api.deleteCard(card.id),
      'Cartão excluído.',
    );
  }

  confirmDelete(includeFuture = false) {
    const dialog = this.deleteDialog;
    if (!dialog) return;
    this.save(includeFuture && dialog.futureRequest ? dialog.futureRequest : dialog.request, () => {
      this.deleteDialog = null;
      this.load();
    }, includeFuture ? (dialog.futureSuccessMessage ?? dialog.successMessage) : dialog.successMessage);
  }

  cancelDelete() {
    this.deleteDialog = null;
  }

  cardLabel(id: number) {
    return this.cards.find((card) => card.id === id)?.name ?? 'Cartão';
  }

  categoryLabel(id: number | null) {
    return this.categories.find((category) => category.id === id)?.name ?? 'Categoria';
  }

  logout() {
    localStorage.removeItem('financeiro-rhc-token');
    localStorage.removeItem('financeiro-rhc-name');
    void this.router.navigateByUrl('/');
  }

  private load() {
    this.loading = true;
    this.error = '';
    this.api.getExpenses(this.year, this.month).pipe(
      switchMap((expenses) => forkJoin({
        incomes: this.api.getIncomes(),
        categories: this.api.getCategories(),
        cards: this.api.getCards(),
        purchases: this.api.getCardPurchases(this.year, this.month),
        benefitEntries: this.api.getBenefitEntries(),
        summary: this.api.getMonthlySummary(this.year, this.month),
      }).pipe(map((data) => ({ ...data, expenses })))),
    ).subscribe({
      next: (data) => {
        this.incomes = data.incomes;
        this.expenses = data.expenses;
        this.categories = data.categories;
        this.cards = data.cards;
        this.purchases = data.purchases;
        this.benefitEntries = data.benefitEntries;
        this.summary = data.summary;
        this.openingBalance = data.summary.openingBalance;
        if (!this.purchaseForm.creditCardId && this.cards.length) this.purchaseForm.creditCardId = this.cards[0].id;
        if (!this.expenseForm.categoryId) {
          this.expenseForm.categoryId = this.categories.find((category) => category.type === 'expense')?.id ?? 0;
        }
        if (!this.purchaseForm.categoryId) {
          this.purchaseForm.categoryId = this.categories.find((category) => category.type === 'expense')?.id ?? 0;
        }
        this.loading = false;
        this.changeDetector.markForCheck();
      },
      error: (error: unknown) => this.failed(error),
    });
  }

  private loadAnnual() {
    this.loading = true;
    this.api.getAnnualSummary(this.year).subscribe({
      next: (summary) => {
        this.annual = summary;
        this.loading = false;
        this.changeDetector.markForCheck();
      },
      error: (error: unknown) => this.failed(error),
    });
  }

  private save<T>(request: Observable<T>, after: (response: T) => void, message: string) {
    this.saving = true;
    this.error = '';
    this.notice = '';
    request.subscribe({
      next: (response) => {
        after(response);
        this.notice = message;
        this.saving = false;
        this.changeDetector.markForCheck();
      },
      error: (error) => this.failed(error),
    });
  }

  private askDelete(
    message: string,
    request: Observable<unknown>,
    successMessage: string,
    futureRequest?: Observable<unknown>,
    futureSuccessMessage?: string,
  ) {
    this.error = '';
    this.deleteDialog = { message, request, successMessage, futureRequest, futureSuccessMessage };
  }

  private done(message: string) {
    this.notice = message;
    this.saving = false;
  }

  private failed(error: unknown) {
    const response = error as { error?: { message?: string }; message?: string };
    this.error = response.error?.message ?? response.message ?? 'Não foi possível concluir. Verifique a conexão com a API e tente novamente.';
    this.loading = false;
    this.saving = false;
    this.changeDetector.markForCheck();
  }

  private inMonth(value: string) {
    const date = new Date(`${value}T00:00:00`);
    return date.getFullYear() === this.year && date.getMonth() + 1 === this.month;
  }

  private emptyIncome(): IncomeInput {
    return { description: '', accountName: '', categoryName: 'Salário', receivedAt: this.dateValue(), amount: 0 };
  }

  private emptyExpense(): ExpenseInput {
    return {
      description: '',
      categoryId: 0,
      expenseDate: this.dateValue(),
      amount: 0,
      isPaid: false,
      recurrenceType: 0,
      startInstallment: 1,
      totalInstallments: null,
    };
  }

  private emptyPurchase(): CardPurchaseInput {
    return {
      creditCardId: 0,
      categoryId: 0,
      description: '',
      purchaseDate: this.dateValue(),
      amount: 0,
      recurrenceType: 0,
      startInstallment: 1,
      totalInstallments: null,
    };
  }

  private emptyBenefit(): BenefitEntryInput {
    return { description: '', entryDate: this.dateValue(), amount: 0, type: 0 };
  }

  private dateValue() {
    const now = new Date();
    return `${now.getFullYear()}-${String(now.getMonth() + 1).padStart(2, '0')}-${String(now.getDate()).padStart(2, '0')}`;
  }

  barHeight(value: number) {
    const max = Math.max(...(this.annual?.months.map((item) => Math.max(item.incomeTotal, item.expensesTotal)) ?? [0]), 1);
    return Math.max(value > 0 ? 6 : 0, (value / max) * 100);
  }

  annualAreas() {
    if (!this.annual) return [];
    const areas = [
      { label: 'Receitas', amount: this.annual.incomeTotal, color: 'var(--verde)' },
      { label: 'Despesas', amount: this.annual.expensesTotal, color: 'var(--laranja)' },
      { label: 'Cartão de crédito', amount: this.annual.creditCardTotal, color: 'var(--vermelho)' },
      { label: 'Vale alimentação', amount: this.annual.benefitPurchaseTotal, color: 'var(--oliva)' },
    ];
    const total = areas.reduce((sum, item) => sum + item.amount, 0);
    return areas.map((item) => ({ ...item, percentage: total ? (item.amount / total) * 100 : 0 }));
  }
}
