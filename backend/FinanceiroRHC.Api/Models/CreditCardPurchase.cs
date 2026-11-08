using System.Text.Json.Serialization;
using FinanceiroRHC.Api.Services;

namespace FinanceiroRHC.Api.Models;

public class CreditCardPurchase : IUserOwnedEntity
{
    public int Id { get; set; }
    public required string UserId { get; set; }
    public int CreditCardId { get; set; }
    [JsonIgnore]
    public CreditCard? CreditCard { get; set; }
    public int? CategoryId { get; set; }
    public required string Description { get; set; }
    public DateOnly PurchaseDate { get; set; }
    public decimal Amount { get; set; }
    public int? RecurrenceRuleId { get; set; }
    [JsonIgnore]
    public CardPurchaseRecurrenceRule? RecurrenceRule { get; set; }
    public int? InstallmentNumber { get; set; }
    public int? TotalInstallments { get; set; }
    public bool IsDeleted { get; set; }
}
