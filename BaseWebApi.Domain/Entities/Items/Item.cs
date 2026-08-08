using BaseWebApi.Domain.Aggregates;
using BaseWebApi.Domain.DomainEvents.Items;
using BaseWebApi.Domain.Enums;
using BaseWebApi.Shared.Exceptions;

namespace BaseWebApi.Domain.Entities.Items;

/// <summary>
/// Sample aggregate used as the reusable Items feature template.
/// TODO: Clone this pattern for CRM/ERP/HR entities.
/// </summary>
public sealed class Item : BaseAggregateRoot<Guid>
{
    private Item()
    {
    }

    public string Name { get; private set; } = string.Empty;

    public string? Description { get; private set; }

    public Status Status { get; private set; } = Status.Draft;

    public Priority Priority { get; private set; } = Priority.Medium;

    public string? AttachmentObjectKey { get; private set; }

    public static Item Create(
        string name,
        string? description = null,
        Priority priority = Priority.Medium,
        string? createdBy = null)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainException("Item name is required.", "item.name_required");
        }

        var item = new Item
        {
            Id = Guid.NewGuid(),
            Name = name.Trim(),
            Description = description?.Trim(),
            Status = Status.Active,
            Priority = priority
        };

        item.MarkCreated(createdBy);
        item.RaiseDomainEvent(new ItemCreatedEvent(item.Id, item.Name));
        return item;
    }

    public void Update(string name, string? description, Status status, Priority priority, string? updatedBy = null)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainException("Item name is required.", "item.name_required");
        }

        Name = name.Trim();
        Description = description?.Trim();
        Status = status;
        Priority = priority;
        MarkUpdated(updatedBy);
    }

    public void SetAttachment(string objectKey, string? updatedBy = null)
    {
        if (string.IsNullOrWhiteSpace(objectKey))
        {
            throw new DomainException("Attachment key is required.", "item.attachment_required");
        }

        AttachmentObjectKey = objectKey;
        MarkUpdated(updatedBy);
    }

    public void Archive(string? updatedBy = null)
    {
        Status = Status.Archived;
        MarkUpdated(updatedBy);
    }
}
