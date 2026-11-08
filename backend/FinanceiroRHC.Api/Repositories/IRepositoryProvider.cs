namespace FinanceiroRHC.Api.Repositories;

public interface IRepositoryProvider
{
    IRepository<T> For<T>() where T : class, Models.IUserOwnedEntity;
}

public class RepositoryProvider(IServiceProvider services) : IRepositoryProvider
{
    public IRepository<T> For<T>() where T : class, Models.IUserOwnedEntity =>
        services.GetRequiredService<IRepository<T>>();
}
