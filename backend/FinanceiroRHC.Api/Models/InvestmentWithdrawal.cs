namespace FinanceiroRHC.Api.Models;

public class InvestmentWithdrawal : IUserOwnedEntity
{
    public int Id { get; set; }
    public required string UserId { get; set; }
    public int InvestmentId { get; set; }
    public Investment? Investment { get; set; }
    public DateOnly WithdrawalDate { get; set; }
    public decimal Amount { get; set; }
    public decimal ReturnAmount { get; set; }
}
