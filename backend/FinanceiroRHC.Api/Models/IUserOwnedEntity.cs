namespace FinanceiroRHC.Api.Models;

public interface IUserOwnedEntity
{
    int Id { get; set; }
    string UserId { get; set; }
}
