import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import {
  AnnualSummary,
  AuthResponse,
  BenefitEntry,
  BenefitEntryInput,
  CardInvoicePayment,
  CardPurchase,
  CardPurchaseInput,
  Category,
  CreditCard,
  Expense,
  ExpenseInput,
  Income,
  IncomeInput,
  Investment,
  InvestmentInput,
  InvestmentWithdrawal,
  InvestmentWithdrawalInput,
  MonthlySummary,
} from './models';

const API_URL = 'http://localhost:5002/api';

@Injectable({ providedIn: 'root' })
export class ApiService {
  private readonly http = inject(HttpClient);

  login(email: string, password: string) {
    return this.http.post<AuthResponse>(`${API_URL}/auth/login`, { email, password });
  }

  register(data: {
    name: string;
    email: string;
    password: string;
    companyName: string;
    creditCards: string[];
    benefitName: string;
  }) {
    return this.http.post<AuthResponse>(`${API_URL}/auth/register`, data);
  }

  getIncomes() {
    return this.http.get<Income[]>(`${API_URL}/incomes`);
  }

  saveIncome(data: IncomeInput, id?: number) {
    return id
      ? this.http.put<Income>(`${API_URL}/incomes/${id}`, data)
      : this.http.post<Income>(`${API_URL}/incomes`, data);
  }

  deleteIncome(id: number) {
    return this.http.delete<boolean>(`${API_URL}/incomes/${id}`);
  }

  getExpenses(year: number, month: number) {
    return this.http.get<Expense[]>(`${API_URL}/expenses`, { params: { year, month } });
  }

  saveExpense(data: ExpenseInput, id?: number) {
    return id
      ? this.http.put<Expense>(`${API_URL}/expenses/${id}`, data)
      : this.http.post<Expense>(`${API_URL}/expenses`, data);
  }

  deleteExpense(id: number) {
    return this.http.delete<boolean>(`${API_URL}/expenses/${id}`);
  }

  cancelExpenseRecurrenceFrom(id: number) {
    return this.http.delete<void>(`${API_URL}/expenses/${id}/recurrence`);
  }

  getCategories() {
    return this.http.get<Category[]>(`${API_URL}/categories`);
  }

  saveCategory(data: { name: string; type: string }, id?: number) {
    return id
      ? this.http.put<Category>(`${API_URL}/categories/${id}`, data)
      : this.http.post<Category>(`${API_URL}/categories`, data);
  }

  deleteCategory(id: number) {
    return this.http.delete<boolean>(`${API_URL}/categories/${id}`);
  }

  getCards() {
    return this.http.get<CreditCard[]>(`${API_URL}/cards`);
  }

  saveCard(data: { name: string }, id?: number) {
    return id
      ? this.http.put<CreditCard>(`${API_URL}/cards/${id}`, data)
      : this.http.post<CreditCard>(`${API_URL}/cards`, data);
  }

  deleteCard(id: number) {
    return this.http.delete<void>(`${API_URL}/cards/${id}`);
  }

  getCardPurchases(year: number, month: number) {
    return this.http.get<CardPurchase[]>(`${API_URL}/card-purchases`, { params: { year, month } });
  }

  getCardInvoicePayment(year: number, month: number) {
    return this.http.get<CardInvoicePayment>(`${API_URL}/card-invoice-payment`, { params: { year, month } });
  }

  payCardInvoice(year: number, month: number, paidAt: string) {
    return this.http.post<CardInvoicePayment>(`${API_URL}/card-invoice-payment`, { year, month, paidAt });
  }

  saveCardPurchase(data: CardPurchaseInput, id?: number) {
    return id
      ? this.http.put<CardPurchase>(`${API_URL}/card-purchases/${id}`, data)
      : this.http.post<CardPurchase>(`${API_URL}/card-purchases`, data);
  }

  deleteCardPurchase(id: number) {
    return this.http.delete<boolean>(`${API_URL}/card-purchases/${id}`);
  }

  cancelCardPurchaseRecurrenceFrom(id: number) {
    return this.http.delete<void>(`${API_URL}/card-purchases/${id}/recurrence`);
  }

  getBenefitEntries() {
    return this.http.get<BenefitEntry[]>(`${API_URL}/benefit-entries`);
  }

  saveBenefitEntry(data: BenefitEntryInput, id?: number) {
    return id
      ? this.http.put<BenefitEntry>(`${API_URL}/benefit-entries/${id}`, data)
      : this.http.post<BenefitEntry>(`${API_URL}/benefit-entries`, data);
  }

  deleteBenefitEntry(id: number) {
    return this.http.delete<boolean>(`${API_URL}/benefit-entries/${id}`);
  }

  getInvestments() {
    return this.http.get<Investment[]>(`${API_URL}/investments`);
  }

  saveInvestment(data: InvestmentInput, id?: number) {
    return id
      ? this.http.put<Investment>(`${API_URL}/investments/${id}`, data)
      : this.http.post<Investment>(`${API_URL}/investments`, data);
  }

  deleteInvestment(id: number) {
    return this.http.delete<boolean>(`${API_URL}/investments/${id}`);
  }

  withdrawInvestment(id: number, data: InvestmentWithdrawalInput) {
    return this.http.post<InvestmentWithdrawal>(`${API_URL}/investments/${id}/withdrawals`, data);
  }

  deleteInvestmentWithdrawal(investmentId: number, withdrawalId: number) {
    return this.http.delete<void>(`${API_URL}/investments/${investmentId}/withdrawals/${withdrawalId}`);
  }

  getMonthlySummary(year: number, month: number) {
    return this.http.get<MonthlySummary>(`${API_URL}/summary/monthly`, { params: { year, month } });
  }

  saveOpeningBalance(year: number, month: number, amount: number) {
    return this.http.put<MonthlySummary>(`${API_URL}/summary/opening-balance`, { year, month, amount });
  }

  getAnnualSummary(year: number) {
    return this.http.get<AnnualSummary>(`${API_URL}/summary/annual`, { params: { year } });
  }
}
