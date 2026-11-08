using FinanceiroRHC.Api.Models;

namespace FinanceiroRHC.Api.Repositories;

public interface IRepository<T> where T : class, IUserOwnedEntity
{
    Task<List<T>> GetByUserAsync(string userId, CancellationToken cancellationToken);
    Task<T?> GetByIdAsync(int id, string userId, CancellationToken cancellationToken);
    Task AddAsync(T entity, CancellationToken cancellationToken);
    void Remove(T entity);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
