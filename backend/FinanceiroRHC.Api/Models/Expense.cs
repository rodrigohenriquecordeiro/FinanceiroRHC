using System.Text.Json.Serialization;

namespace FinanceiroRHC.Api.Models;

public class Expense : IUserOwnedEntity
{
    public int Id { get; set; }
    public required string UserId { get; set; }
    public required string Description { get; set; }
    public int CategoryId { get; set; }
    [JsonIgnore]
    public FinancialCategory? Category { get; set; }
    public DateOnly ExpenseDate { get; set; }
    public decimal Amount { get; set; }
    public bool IsPaid { get; set; }
    public int? RecurrenceRuleId { get; set; }
    [JsonIgnore]
    public ExpenseRecurrenceRule? RecurrenceRule { get; set; }
    public int? InstallmentNumber { get; set; }
    public int? TotalInstallments { get; set; }
    public bool IsDeleted { get; set; }
}
