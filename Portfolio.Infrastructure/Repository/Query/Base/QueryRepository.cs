
using Microsoft.EntityFrameworkCore;
using Portfolio.Domain.Repositories.Query.Base;
using Portfolio.Infra.Data;

namespace Portfolio.Infra.Repository.Query.Base;

public class QueryRepository<T> : IQueryRepository<T> where T : class
{
    protected readonly ApplicationDbContext _context;

    public QueryRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<T?> GetByIdAsync(
        object id,
        CancellationToken cancellationToken)
    {
        return await _context.Set<T>().FindAsync(
            [id],
            cancellationToken);
    }

    public async Task<IReadOnlyList<T>> GetAllAsync(
        CancellationToken cancellationToken)
    {
        return await _context.Set<T>()
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> ExistsAsync(
        object id,
        CancellationToken cancellationToken)
    {
        return await _context.Set<T>().FindAsync(
            [id],
            cancellationToken) is not null;
    }
}
