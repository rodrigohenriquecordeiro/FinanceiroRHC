namespace FinanceiroRHC.Api.Models;

public class BenefitEntry : IUserOwnedEntity
{
    public int Id { get; set; }
    public required string UserId { get; set; }
    public required string Description { get; set; }
    public DateOnly EntryDate { get; set; }
    public decimal Amount { get; set; }
    public BenefitEntryType Type { get; set; }
}

public enum BenefitEntryType
{
    Recharge,
    Purchase
}
