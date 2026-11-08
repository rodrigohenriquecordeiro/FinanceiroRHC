using FinanceiroRHC.Api.Models;
using FinanceiroRHC.Api.Services;

namespace FinanceiroRHC.Tests;

public class FinanceCalculatorTests
{
    [Fact]
    public void Calculate_SeparatesPaidAndProjectedExpenses()
    {
        var incomes = new[]
        {
            new Income
            {
                UserId = "user-1",
                Description = "Salário",
                AccountName = "Conta",
                CategoryName = "Salário",
                ReceivedAt = new DateOnly(2026, 11, 5),
                Amount = 5000
            }
        };
        var expenses = new[]
        {
            new Expense
            {
                UserId = "user-1",
                Description = "Aluguel",
                CategoryId = 1,
                ExpenseDate = new DateOnly(2026, 11, 6),
                Amount = 1200,
                IsPaid = true
            },
            new Expense
            {
                UserId = "user-1",
                Description = "Conta de luz",
                CategoryId = 1,
                ExpenseDate = new DateOnly(2026, 11, 8),
                Amount = 300,
                IsPaid = false
            }
        };

        var summary = FinanceCalculator.Calculate(2026, 11, 800, incomes, expenses);

        Assert.Equal(5000, summary.IncomeTotal);
        Assert.Equal(1500, summary.ExpensesTotal);
        Assert.Equal(1200, summary.PaidExpensesTotal);
        Assert.Equal(4600, summary.CurrentBalance);
        Assert.Equal(4300, summary.ProjectedBalance);
    }

    [Fact]
    public void Calculate_WithNoTransactions_PreservesOpeningBalance()
    {
        var summary = FinanceCalculator.Calculate(2026, 11, 123.45m, [], []);

        Assert.Equal(123.45m, summary.CurrentBalance);
        Assert.Equal(123.45m, summary.ProjectedBalance);
    }

    [Theory]
    [InlineData(2026, 1, 31, 2, 2026, 2, 28)]
    [InlineData(2024, 1, 31, 2, 2024, 2, 29)]
    [InlineData(2026, 11, 8, 3, 2027, 1, 8)]
    public void RecurrenceSchedule_ClampsMonthEndAndAdvancesInstallments(
        int startYear,
        int startMonth,
        int startDay,
        int installmentNumber,
        int expectedYear,
        int expectedMonth,
        int expectedDay)
    {
        var rule = new ExpenseRecurrenceRule
        {
            UserId = "user-1",
            Description = "Despesa recorrente",
            CategoryId = 1,
            StartDate = new DateOnly(startYear, startMonth, startDay),
            Amount = 100,
            RecurrenceType = ExpenseRecurrenceType.Unlimited,
            StartInstallment = 1
        };

        var date = ExpenseRecurrenceSchedule.GetInstallmentDate(rule, installmentNumber);

        Assert.Equal(new DateOnly(expectedYear, expectedMonth, expectedDay), date);
    }

    [Fact]
    public void CardPurchaseRecurrenceSchedule_ClampsMonthEnd()
    {
        var rule = new CardPurchaseRecurrenceRule
        {
            UserId = "user-1",
            CreditCardId = 1,
            CategoryId = 1,
            Description = "Compra recorrente",
            StartDate = new DateOnly(2024, 1, 31),
            Amount = 75,
            RecurrenceType = ExpenseRecurrenceType.Installments,
            StartInstallment = 2,
            TotalInstallments = 5
        };

        var date = ExpenseRecurrenceSchedule.GetInstallmentDate(rule, 3);

        Assert.Equal(new DateOnly(2024, 2, 29), date);
    }
}
