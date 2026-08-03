namespace Portfolio.Domain.Entities;

/// <summary>
/// Provides common properties shared across domain entities.
/// </summary>
public abstract class BaseEntity
{
    public long Id { get; set; }

    public DateTime CreatedDate { get; set; }

    public DateTime ModifiedDate { get; private set; } = DateTime.UtcNow;

    protected BaseEntity()
    {
    }

    public void UpdateModifiedDate()
    {
        ModifiedDate = DateTime.UtcNow;
    }
}