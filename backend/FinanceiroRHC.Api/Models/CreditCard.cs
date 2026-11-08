using System.Text.Json.Serialization;

namespace FinanceiroRHC.Api.Models;

public class CreditCard : IUserOwnedEntity
{
    public int Id { get; set; }
    public required string UserId { get; set; }
    public required string Name { get; set; }
    [JsonIgnore]
    public ICollection<CreditCardPurchase> Purchases { get; set; } = [];
}
