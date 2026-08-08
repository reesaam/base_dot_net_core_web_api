using System.ComponentModel;
using ProtoBuf;

namespace BaseWebApi.Application.DTOs.Common;

[ProtoContract]
public sealed record PaginationRequestDto
{
    [ProtoMember(1)]
    [DefaultValue(1)]
    public int Page { get; init; } = 1;

    [ProtoMember(2)]
    [DefaultValue(20)]
    public int PageSize { get; init; } = 20;

    [ProtoMember(3)]
    public string? SortBy { get; init; }

    [ProtoMember(4)]
    public string? SortDirection { get; init; }

    [ProtoMember(5)]
    public string? Search { get; init; }
}

[ProtoContract]
public sealed record PaginationResponseDto<T>
{
    [ProtoMember(1, IsRequired = true)]
    public IList<T> Items { get; init; } = [];

    [ProtoMember(2)]
    public int Page { get; init; }

    [ProtoMember(3)]
    public int PageSize { get; init; }

    [ProtoMember(4)]
    public long TotalCount { get; init; }

    [ProtoMember(5)]
    public int TotalPages { get; init; }
}
