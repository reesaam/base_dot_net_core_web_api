using BaseWebApi.Domain.DomainEvents;

namespace BaseWebApi.Domain.DomainEvents.Items;

public sealed record ItemCreatedEvent(Guid ItemId, string Name) : BaseDomainEvent;
