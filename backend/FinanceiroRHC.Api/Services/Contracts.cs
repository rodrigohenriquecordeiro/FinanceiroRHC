using System.ComponentModel.DataAnnotations;
using FinanceiroRHC.Api.Models;

namespace FinanceiroRHC.Api.Services;

public sealed record RegisterRequest(
    [param: Required, StringLength(100)] string Name,
    [param: Required, EmailAddress] string Email,
    [param: Required, MinLength(8)] string Password,
    [param: StringLength(120)] string? CompanyName,
    [param: Required, MinLength(1)] string[] CreditCards,
    [param: Required, StringLength(80)] string BenefitName);

public sealed record LoginRequest([param: Required, EmailAddress] string Email, [param: Required] string Password);
public sealed record AuthResponse(string Token, string Name, string Email);
public sealed record IncomeRequest(
    [param: Required, StringLength(160)] string Description,
    [param: Required, StringLength(100)] string AccountName,
    [param: Required, StringLength(80)] string CategoryName,
    DateOnly ReceivedAt,
    [param: Range(0.01, 999999999)] decimal Amount,
    IncomeType Type = IncomeType.Regular);
public sealed record ExpenseRequest(
    [param: Required, StringLength(160)] string Description,
    int CategoryId,
    DateOnly ExpenseDate,
    [param: Range(0.01, 999999999)] decimal Amount,
    bool IsPaid,
    ExpenseRecurrenceType RecurrenceType = ExpenseRecurrenceType.None,
    [param: Range(1, 1200)] int StartInstallment = 1,
    [param: Range(1, 1200)] int? TotalInstallments = null);
public sealed record ExpenseResponse(
    int Id,
    string Description,
    int CategoryId,
    DateOnly ExpenseDate,
    decimal Amount,
    bool IsPaid,
    ExpenseRecurrenceType RecurrenceType,
    int? InstallmentNumber,
    int? TotalInstallments,
    bool IsInvestmentAdjustment = false,
    bool IsInvoicePayment = false);
public sealed record CategoryRequest(
    [param: Required, StringLength(80)] string Name,
    [param: RegularExpression("^(expense|income)$")] string Type = "expense");
public sealed record CardRequest([param: Required, StringLength(80)] string Name);
public sealed record CreditCardPurchaseRequest(
    int CreditCardId,
    int CategoryId,
    [param: Required, StringLength(160)] string Description,
    DateOnly PurchaseDate,
    [param: Range(0.01, 999999999)] decimal Amount,
    ExpenseRecurrenceType RecurrenceType = ExpenseRecurrenceType.None,
    [param: Range(1, 1200)] int StartInstallment = 1,
    [param: Range(1, 1200)] int? TotalInstallments = null);
public sealed record CardPurchaseResponse(
    int Id,
    int CreditCardId,
    int? CategoryId,
    string Description,
    DateOnly PurchaseDate,
    decimal Amount,
    ExpenseRecurrenceType RecurrenceType,
    int? InstallmentNumber,
    int? TotalInstallments);
public sealed record BenefitEntryRequest(
    [param: Required, StringLength(160)] string Description,
    DateOnly EntryDate,
    [param: Range(0.01, 999999999)] decimal Amount,
    BenefitEntryType Type);
public sealed record CreditCardInvoicePaymentRequest(int Year, int Month, DateOnly PaidAt);
public sealed record CreditCardInvoicePaymentResponse(
    bool IsPaid,
    decimal Amount,
    DateOnly? PaidAt);
public sealed record InvestmentRequest(
    [param: Required, StringLength(160)] string Description,
    DateOnly ApplicationDate,
    DateOnly MaturityDate,
    [param: Range(0.01, 999999999)] decimal Amount);
public sealed record InvestmentWithdrawalRequest(
    DateOnly WithdrawalDate,
    [param: Range(0.01, 999999999)] decimal Amount,
    [param: Range(-999999999, 999999999)] decimal ReturnAmount = 0);
public sealed record InvestmentWithdrawalResponse(
    int Id,
    DateOnly WithdrawalDate,
    decimal Amount,
    decimal ReturnAmount);
public sealed record InvestmentResponse(
    int Id,
    string Description,
    DateOnly ApplicationDate,
    DateOnly MaturityDate,
    decimal Amount,
    decimal WithdrawnAmount,
    decimal CurrentTotal,
    IReadOnlyList<InvestmentWithdrawalResponse> Withdrawals);
public sealed record BalanceOverrideRequest(int Year, int Month, decimal Amount);
public sealed record MonthlySummary(
    int Year,
    int Month,
    decimal OpeningBalance,
    decimal IncomeTotal,
    decimal ExpensesTotal,
    decimal PaidExpensesTotal,
    decimal CurrentBalance,
    decimal ProjectedBalance);
public sealed record AnnualSummary(
    int Year,
    decimal IncomeTotal,
    decimal ExpensesTotal,
    decimal CreditCardTotal,
    decimal BenefitRechargeTotal,
    decimal BenefitPurchaseTotal,
    IReadOnlyList<MonthlySummary> Months);

public static class FinanceCalculator
{
    public static MonthlySummary Calculate(
        int year,
        int month,
        decimal openingBalance,
        IEnumerable<Income> incomes,
        IEnumerable<Expense> expenses)
    {
        var incomeTotal = incomes.Sum(income => income.Amount);
        var expensesTotal = expenses.Sum(expense => expense.Amount);
        var paidExpensesTotal = expenses.Where(expense => expense.IsPaid).Sum(expense => expense.Amount);
        return new MonthlySummary(
            year,
            month,
            openingBalance,
            incomeTotal,
            expensesTotal,
            paidExpensesTotal,
            openingBalance + incomeTotal - paidExpensesTotal,
            openingBalance + incomeTotal - expensesTotal);
    }
}
