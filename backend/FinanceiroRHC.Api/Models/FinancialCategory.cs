using System.Text.Json.Serialization;

namespace FinanceiroRHC.Api.Models;

public class FinancialCategory : IUserOwnedEntity
{
    public int Id { get; set; }
    public required string UserId { get; set; }
    public required string Name { get; set; }
    public string Type { get; set; } = "expense";
    [JsonIgnore]
    public ICollection<Expense> Expenses { get; set; } = [];
}
