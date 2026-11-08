namespace FinanceiroRHC.Api.Models;

public class Investment : IUserOwnedEntity
{
    public int Id { get; set; }
    public required string UserId { get; set; }
    public required string Description { get; set; }
    public DateOnly ApplicationDate { get; set; }
    public DateOnly MaturityDate { get; set; }
    public decimal Amount { get; set; }
    public ICollection<InvestmentWithdrawal> Withdrawals { get; set; } = [];
}
