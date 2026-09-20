namespace BaseWebApi.Shared.Results;

/// <summary>
/// Generic paged response envelope.
/// </summary>
public sealed class PaginationResult<T>
{
    public required IReadOnlyList<T> Items { get; init; }
    public required int Page { get; init; }
    public required int PageSize { get; init; }
    public required long TotalCount { get; init; }

    public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);

    public bool HasPrevious => Page > 1;
    public bool HasNext => Page < TotalPages;

    public static PaginationResult<T> Create(
        IReadOnlyList<T> items,
        int page,
        int pageSize,
        long totalCount) =>
        new()
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };
}