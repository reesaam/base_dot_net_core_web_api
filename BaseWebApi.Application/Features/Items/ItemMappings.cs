using BaseWebApi.Application.DTOs.Items;
using BaseWebApi.Domain.Entities.Items;

namespace BaseWebApi.Application.Features.Items;

public static class ItemMappings
{
    public static ItemDto ToDto(this Item item) =>
        new()
        {
            Id = item.Id,
            Name = item.Name,
            Description = item.Description,
            Status = item.Status.ToString(),
            Priority = item.Priority.ToString(),
            AttachmentObjectKey = item.AttachmentObjectKey,
            CreatedAt = item.CreatedAt,
            UpdatedAt = item.UpdatedAt
        };
}
