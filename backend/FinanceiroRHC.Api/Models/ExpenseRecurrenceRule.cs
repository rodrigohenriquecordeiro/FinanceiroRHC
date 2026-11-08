namespace FinanceiroRHC.Api.Models;

public class ExpenseRecurrenceRule : IUserOwnedEntity
{
    public int Id { get; set; }
    public required string UserId { get; set; }
    public required string Description { get; set; }
    public int CategoryId { get; set; }
    public FinancialCategory? Category { get; set; }
    public DateOnly StartDate { get; set; }
    public decimal Amount { get; set; }
    public ExpenseRecurrenceType RecurrenceType { get; set; }
    public int StartInstallment { get; set; }
    public int? TotalInstallments { get; set; }
    public int? EndInstallment { get; set; }
    public bool IsActive { get; set; } = true;
    public ICollection<Expense> Expenses { get; set; } = [];
}

public enum ExpenseRecurrenceType
{
    None,
    Installments,
    Unlimited
}
