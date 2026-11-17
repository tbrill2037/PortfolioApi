
namespace Portfolio.Domain.Repositories.Query.Base;

public interface IQueryRepository<T>
    where T : class
{
    Task<T?> GetByIdAsync(object id, CancellationToken cancellationToken);

    Task<IReadOnlyList<T>> GetAllAsync(CancellationToken cancellationToken);

    Task<bool> ExistsAsync(object id, CancellationToken cancellationToken);
}
