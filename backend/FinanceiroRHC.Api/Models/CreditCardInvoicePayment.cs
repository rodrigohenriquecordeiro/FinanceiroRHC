namespace FinanceiroRHC.Api.Models;

public class CreditCardInvoicePayment : IUserOwnedEntity
{
    public int Id { get; set; }
    public required string UserId { get; set; }
    public int Year { get; set; }
    public int Month { get; set; }
    public decimal Amount { get; set; }
    public DateOnly PaidAt { get; set; }
    public int ExpenseId { get; set; }
    public Expense? Expense { get; set; }
}
