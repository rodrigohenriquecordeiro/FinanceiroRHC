using FinanceiroRHC.Api.Models;

namespace FinanceiroRHC.Api.Services;

public interface IFinanceService
{
    Task<List<T>> GetAsync<T>(string userId, CancellationToken cancellationToken) where T : class, IUserOwnedEntity;
    Task<T?> GetByIdAsync<T>(int id, string userId, CancellationToken cancellationToken) where T : class, IUserOwnedEntity;
    Task<T> SaveAsync<T>(T entity, int? id, string userId, CancellationToken cancellationToken) where T : class, IUserOwnedEntity;
    Task<bool> DeleteAsync<T>(int id, string userId, CancellationToken cancellationToken) where T : class, IUserOwnedEntity;
    Task<List<ExpenseResponse>> GetExpensesForMonthAsync(string userId, int year, int month, CancellationToken cancellationToken);
    Task<ExpenseResponse> SaveExpenseAsync(ExpenseRequest request, int? id, string userId, CancellationToken cancellationToken);
    Task<bool> CancelRecurringExpensesFromAsync(int expenseId, string userId, CancellationToken cancellationToken);
    Task<bool> HasExpensesUsingCategoryAsync(int categoryId, string userId, CancellationToken cancellationToken);
    Task<List<CardPurchaseResponse>> GetCardPurchasesForMonthAsync(string userId, int year, int month, CancellationToken cancellationToken);
    Task<CardPurchaseResponse> SaveCardPurchaseAsync(CreditCardPurchaseRequest request, int? id, string userId, CancellationToken cancellationToken);
    Task<bool> CancelRecurringCardPurchasesFromAsync(int purchaseId, string userId, CancellationToken cancellationToken);
    Task<CreditCardInvoicePaymentResponse> GetCardInvoicePaymentAsync(string userId, int year, int month, CancellationToken cancellationToken);
    Task<CreditCardInvoicePaymentResponse> PayCardInvoiceAsync(string userId, CreditCardInvoicePaymentRequest request, CancellationToken cancellationToken);
    Task<List<InvestmentResponse>> GetInvestmentsAsync(string userId, CancellationToken cancellationToken);
    Task<InvestmentResponse> SaveInvestmentAsync(InvestmentRequest request, int? id, string userId, CancellationToken cancellationToken);
    Task<InvestmentWithdrawalResponse> WithdrawInvestmentAsync(int investmentId, InvestmentWithdrawalRequest request, string userId, CancellationToken cancellationToken);
    Task<bool> DeleteInvestmentWithdrawalAsync(int investmentId, int withdrawalId, string userId, CancellationToken cancellationToken);
    Task<MonthlySummary> GetMonthlySummaryAsync(string userId, int year, int month, CancellationToken cancellationToken);
    Task<MonthlySummary> SaveOpeningBalanceAsync(string userId, int year, int month, decimal amount, CancellationToken cancellationToken);
    Task<AnnualSummary> GetAnnualSummaryAsync(string userId, int year, CancellationToken cancellationToken);
}
