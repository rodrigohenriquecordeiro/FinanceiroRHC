using Microsoft.AspNetCore.Identity;

namespace FinanceiroRHC.Api.Models;

public class ApplicationUser : IdentityUser
{
    public required string Name { get; set; }
    public string? CompanyName { get; set; }
    public string BenefitName { get; set; } = "Flash";
    public ICollection<CreditCard> CreditCards { get; set; } = [];
}
