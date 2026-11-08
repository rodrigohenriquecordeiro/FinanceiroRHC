namespace FinanceiroRHC.Api.Models;

public class MonthlyBalanceOverride : IUserOwnedEntity
{
    public int Id { get; set; }
    public required string UserId { get; set; }
    public int Year { get; set; }
    public int Month { get; set; }
    public decimal Amount { get; set; }
}
