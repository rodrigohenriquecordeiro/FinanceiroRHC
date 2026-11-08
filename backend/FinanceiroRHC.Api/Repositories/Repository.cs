using FinanceiroRHC.Api.Data;
using FinanceiroRHC.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace FinanceiroRHC.Api.Repositories;

public class Repository<T>(ApplicationDbContext context) : IRepository<T>
    where T : class, IUserOwnedEntity
{
    private readonly DbSet<T> _entities = context.Set<T>();

    public Task<List<T>> GetByUserAsync(string userId, CancellationToken cancellationToken) =>
        _entities.Where(entity => entity.UserId == userId).ToListAsync(cancellationToken);

    public Task<T?> GetByIdAsync(int id, string userId, CancellationToken cancellationToken) =>
        _entities.FirstOrDefaultAsync(entity => entity.Id == id && entity.UserId == userId, cancellationToken);

    public async Task AddAsync(T entity, CancellationToken cancellationToken) =>
        await _entities.AddAsync(entity, cancellationToken);

    public void Remove(T entity) => _entities.Remove(entity);

    public Task SaveChangesAsync(CancellationToken cancellationToken) =>
        context.SaveChangesAsync(cancellationToken);
}
