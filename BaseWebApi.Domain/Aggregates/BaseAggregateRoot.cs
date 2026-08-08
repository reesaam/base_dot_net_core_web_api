using BaseWebApi.Domain.DomainEvents;

namespace BaseWebApi.Domain.Aggregates;

/// <summary>
/// Aggregate root that collects domain events for later dispatch.
/// </summary>
public abstract class BaseAggregateRoot<TId> : Entities.BaseEntity<TId>
    where TId : notnull
{
    private readonly List<BaseDomainEvent> _domainEvents = [];

    public IReadOnlyCollection<BaseDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    protected void RaiseDomainEvent(BaseDomainEvent domainEvent) =>
        _domainEvents.Add(domainEvent);

    public void ClearDomainEvents() => _domainEvents.Clear();
}
