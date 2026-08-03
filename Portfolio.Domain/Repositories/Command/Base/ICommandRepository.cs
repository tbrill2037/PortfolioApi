namespace Portfolio.Domain.Repositories.Command.Base;

/// <summary>
/// Defines common command repository operations for domain entities.
/// </summary>
/// <typeparam name="T">The entity type.</typeparam>
public interface ICommandRepository<T>
    where T : class
{
    /// <summary>
    /// Adds a new entity.
    /// </summary>
    /// <param name="entity">The entity to add.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    /// <returns>The added entity.</returns>
    Task<T> AddAsync(
        T entity,
        CancellationToken cancellationToken);

    /// <summary>
    /// Updates an existing entity.
    /// </summary>
    /// <param name="entity">The entity to update.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    Task UpdateAsync(
        T entity,
        CancellationToken cancellationToken);

    /// <summary>
    /// Deletes an existing entity.
    /// </summary>
    /// <param name="entity">The entity to delete.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    Task DeleteAsync(
        T entity,
        CancellationToken cancellationToken);
}