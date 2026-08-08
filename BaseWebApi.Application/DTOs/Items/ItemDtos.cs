using System.ComponentModel;
using ProtoBuf;

namespace BaseWebApi.Application.DTOs.Items;

[ProtoContract]
public sealed record ItemDto
{
    [ProtoMember(1)]
    public Guid Id { get; init; }

    [ProtoMember(2)]
    public string Name { get; init; } = string.Empty;

    [ProtoMember(3)]
    public string? Description { get; init; }

    [ProtoMember(4)]
    public string Status { get; init; } = string.Empty;

    [ProtoMember(5)]
    public string Priority { get; init; } = string.Empty;

    [ProtoMember(6)]
    public string? AttachmentObjectKey { get; init; }

    [ProtoMember(7)]
    public DateTimeOffset CreatedAt { get; init; }

    [ProtoMember(8)]
    public DateTimeOffset? UpdatedAt { get; init; }
}

[ProtoContract]
public sealed record CreateItemRequestDto
{
    [ProtoMember(1)]
    public string Name { get; init; } = string.Empty;

    [ProtoMember(2)]
    public string? Description { get; init; }

    [ProtoMember(3)]
    public string? Priority { get; init; }
}

[ProtoContract]
public sealed record UpdateItemRequestDto
{
    [ProtoMember(1)]
    public string Name { get; init; } = string.Empty;

    [ProtoMember(2)]
    public string? Description { get; init; }

    [ProtoMember(3)]
    public string Status { get; init; } = string.Empty;

    [ProtoMember(4)]
    public string Priority { get; init; } = string.Empty;
}

[ProtoContract]
public sealed record GetItemRequestDto
{
    [ProtoMember(1)]
    public Guid Id { get; init; }
}

[ProtoContract]
public sealed record DeleteItemRequestDto
{
    [ProtoMember(1)]
    public Guid Id { get; init; }
}

[ProtoContract]
public sealed record ListItemsRequestDto
{
    [ProtoMember(1)]
    [DefaultValue(1)]
    public int Page { get; init; } = 1;

    [ProtoMember(2)]
    [DefaultValue(20)]
    public int PageSize { get; init; } = 20;

    [ProtoMember(3)]
    public string? Search { get; init; }

    [ProtoMember(4)]
    public string? Status { get; init; }

    [ProtoMember(5)]
    public string? SortBy { get; init; }

    [ProtoMember(6)]
    public string? SortDirection { get; init; }
}

[ProtoContract]
public sealed record ListItemsResponseDto
{
    [ProtoMember(1, IsRequired = true)]
    public IList<ItemDto> Items { get; init; } = [];

    [ProtoMember(2)]
    public int Page { get; init; }

    [ProtoMember(3)]
    public int PageSize { get; init; }

    [ProtoMember(4)]
    public long TotalCount { get; init; }

    [ProtoMember(5)]
    public int TotalPages { get; init; }
}
