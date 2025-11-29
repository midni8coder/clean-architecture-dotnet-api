namespace Domain.Entities;

/// <summary>
/// Base class for all aggregate roots in the domain.
/// Implements DDD aggregate root pattern.
/// </summary>
public abstract class AggregateRoot
{
    public Guid Id { get; protected set; } = Guid.NewGuid();
    public DateTime CreatedAtUtc { get; protected set; } = DateTime.UtcNow;
    public DateTime? UpdatedAtUtc { get; protected set; }

    protected AggregateRoot()
    {
    }

    protected AggregateRoot(Guid id)
    {
        Id = id;
    }
}
