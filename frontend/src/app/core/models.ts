export interface AuthResponse {
  token: string;
  name: string;
  email: string;
}

export interface Income {
  id: number;
  description: string;
  accountName: string;
  categoryName: string;
  receivedAt: string;
  amount: number;
  type: number;
  investmentWithdrawalId: number | null;
}

export interface Expense {
  id: number;
  description: string;
  categoryId: number;
  expenseDate: string;
  amount: number;
  isPaid: boolean;
  recurrenceType: number;
  installmentNumber: number | null;
  totalInstallments: number | null;
  isInvestmentAdjustment: boolean;
  isInvoicePayment: boolean;
}

export interface Category {
  id: number;
  name: string;
  type: string;
}

export interface CreditCard {
  id: number;
  name: string;
}

export interface CardPurchase {
  id: number;
  creditCardId: number;
  categoryId: number | null;
  description: string;
  purchaseDate: string;
  amount: number;
  recurrenceType: number;
  installmentNumber: number | null;
  totalInstallments: number | null;
}

export interface BenefitEntry {
  id: number;
  description: string;
  entryDate: string;
  amount: number;
  type: number;
}

export interface CardInvoicePayment {
  isPaid: boolean;
  amount: number;
  paidAt: string | null;
}

export interface Investment {
  id: number;
  description: string;
  applicationDate: string;
  maturityDate: string;
  amount: number;
  withdrawnAmount: number;
  currentTotal: number;
  withdrawals: InvestmentWithdrawal[];
}

export interface InvestmentWithdrawal {
  id: number;
  withdrawalDate: string;
  amount: number;
  returnAmount: number;
}

export interface MonthlySummary {
  year: number;
  month: number;
  openingBalance: number;
  incomeTotal: number;
  expensesTotal: number;
  paidExpensesTotal: number;
  currentBalance: number;
  projectedBalance: number;
}

export interface AnnualSummary {
  year: number;
  incomeTotal: number;
  expensesTotal: number;
  creditCardTotal: number;
  benefitRechargeTotal: number;
  benefitPurchaseTotal: number;
  months: MonthlySummary[];
}

export interface IncomeInput {
  description: string;
  accountName: string;
  categoryName: string;
  receivedAt: string;
  amount: number;
  type: number;
}

export interface ExpenseInput {
  description: string;
  categoryId: number;
  expenseDate: string;
  amount: number;
  isPaid: boolean;
  recurrenceType: number;
  startInstallment: number;
  totalInstallments: number | null;
}

export interface CardPurchaseInput {
  creditCardId: number;
  categoryId: number;
  description: string;
  purchaseDate: string;
  amount: number;
  recurrenceType: number;
  startInstallment: number;
  totalInstallments: number | null;
}

export interface BenefitEntryInput {
  description: string;
  entryDate: string;
  amount: number;
  type: number;
}

export interface InvestmentInput {
  description: string;
  applicationDate: string;
  maturityDate: string;
  amount: number;
}

export interface InvestmentWithdrawalInput {
  withdrawalDate: string;
  amount: number;
  returnAmount: number;
}
