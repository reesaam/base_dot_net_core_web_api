namespace BaseWebApi.Domain.DomainEvents;

/// <summary>
/// Base domain event raised by aggregate roots.
/// </summary>
public abstract record BaseDomainEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTimeOffset OccurredAt { get; init; } = DateTimeOffset.UtcNow;
}
