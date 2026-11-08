namespace FinanceiroRHC.Api.Models;

public class Income : IUserOwnedEntity
{
    public int Id { get; set; }
    public required string UserId { get; set; }
    public required string Description { get; set; }
    public required string AccountName { get; set; }
    public required string CategoryName { get; set; }
    public DateOnly ReceivedAt { get; set; }
    public decimal Amount { get; set; }
}
