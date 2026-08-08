using BaseWebApi.Domain.Enums;
using BaseWebApi.Shared.Constants;

namespace BaseWebApi.Application.Helpers;

/// <summary>
/// Shared pagination / sorting / filtering helpers.
/// </summary>
public static class QueryHelpers
{
    public static (int Page, int PageSize) NormalizePaging(int page, int pageSize)
    {
        page = page <= 0 ? AppConstants.Pagination.DefaultPage : page;
        pageSize = pageSize <= 0
            ? AppConstants.Pagination.DefaultPageSize
            : Math.Min(pageSize, AppConstants.Pagination.MaxPageSize);
        return (page, pageSize);
    }

    public static SortDirection ParseSortDirection(string? value) =>
        string.Equals(value, "desc", StringComparison.OrdinalIgnoreCase)
            ? SortDirection.Desc
            : SortDirection.Asc;

    public static Status? ParseStatus(string? value) =>
        Enum.TryParse<Status>(value, ignoreCase: true, out var status) ? status : null;

    public static Priority ParsePriority(string? value, Priority fallback = Priority.Medium) =>
        Enum.TryParse<Priority>(value, ignoreCase: true, out var priority) ? priority : fallback;
}
