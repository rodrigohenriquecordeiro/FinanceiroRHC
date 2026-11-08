using FinanceiroRHC.Api.Models;

namespace FinanceiroRHC.Api.Services;

public static class ExpenseRecurrenceSchedule
{
    public static DateOnly GetInstallmentDate(ExpenseRecurrenceRule rule, int installmentNumber)
        => GetInstallmentDate(rule.StartDate, rule.StartInstallment, installmentNumber);

    public static DateOnly GetInstallmentDate(CardPurchaseRecurrenceRule rule, int installmentNumber)
        => GetInstallmentDate(rule.StartDate, rule.StartInstallment, installmentNumber);

    private static DateOnly GetInstallmentDate(DateOnly startDate, int startInstallment, int installmentNumber)
    {
        if (installmentNumber < startInstallment)
        {
            throw new ArgumentOutOfRangeException(nameof(installmentNumber));
        }

        var month = new DateOnly(startDate.Year, startDate.Month, 1)
            .AddMonths(installmentNumber - startInstallment);
        var day = Math.Min(startDate.Day, DateTime.DaysInMonth(month.Year, month.Month));
        return new DateOnly(month.Year, month.Month, day);
    }
}
