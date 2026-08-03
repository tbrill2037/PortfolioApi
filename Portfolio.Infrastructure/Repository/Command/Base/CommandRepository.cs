using Portfolio.Domain.Repositories.Command.Base;
using Portfolio.Infra.Data;

namespace Portfolio.Infra.Repository.Command.Base;

/// <summary>
/// Provides generic command operations for entity persistence.
/// </summary>
public class CommandRepository<T> : ICommandRepository<T> where T : class
{
    protected readonly ApplicationDbContext _context;

    public CommandRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<T> AddAsync(T entity, CancellationToken cancellationToken)
    {
        await _context.Set<T>().AddAsync(entity, cancellationToken);

        return entity;
    }

    public Task UpdateAsync(T entity, CancellationToken cancellationToken)
    {
        _context.Set<T>().Update(entity);

        return Task.CompletedTask;
    }

    public Task DeleteAsync(T entity, CancellationToken cancellationToken)
    {
        _context.Set<T>().Remove(entity);

        return Task.CompletedTask;
    }
}