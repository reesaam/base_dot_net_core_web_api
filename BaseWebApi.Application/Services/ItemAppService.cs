using BaseWebApi.Application.Abstractions.Caching;
using BaseWebApi.Application.Abstractions.Messaging;
using BaseWebApi.Application.Abstractions.Persistence;
using BaseWebApi.Application.Abstractions.Search;
using BaseWebApi.Application.Abstractions.Storage;
using BaseWebApi.Application.DTOs.Items;
using BaseWebApi.Application.Features.Items;
using BaseWebApi.Application.Helpers;
using BaseWebApi.Domain.Entities.Items;
using BaseWebApi.Domain.Enums;
using BaseWebApi.Shared.Constants;
using BaseWebApi.Shared.Helpers;
using BaseWebApi.Shared.Results;
using Microsoft.Extensions.Logging;
using ApplicationException = BaseWebApi.Shared.Exceptions.ApplicationException;

namespace BaseWebApi.Application.Services;

public interface IItemAppService
{
    Task<Result<PaginationResult<ItemDto>>> ListAsync(ListItemsRequestDto request, CancellationToken cancellationToken = default);

    Task<Result<ItemDto>> GetAsync(Guid id, CancellationToken cancellationToken = default);

    Task<Result<ItemDto>> CreateAsync(CreateItemRequestDto request, CancellationToken cancellationToken = default);

    Task<Result<ItemDto>> UpdateAsync(Guid id, UpdateItemRequestDto request, CancellationToken cancellationToken = default);

    Task<Result> DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    Task<Result<ItemDto>> UploadAttachmentAsync(
        Guid id,
        string fileName,
        string contentType,
        Stream content,
        CancellationToken cancellationToken = default);
}

public sealed class ItemAppService : IItemAppService
{
    private readonly IEfRepository<Item> _items;
    private readonly ICacheService _cache;
    private readonly IEventPublisher _publisher;
    private readonly ISearchService _search;
    private readonly IObjectStorage _storage;
    private readonly ILogger<ItemAppService> _logger;

    public ItemAppService(
        IEfRepository<Item> items,
        ICacheService cache,
        IEventPublisher publisher,
        ISearchService search,
        IObjectStorage storage,
        ILogger<ItemAppService> logger)
    {
        _items = items;
        _cache = cache;
        _publisher = publisher;
        _search = search;
        _storage = storage;
        _logger = logger;
    }

    public async Task<Result<PaginationResult<ItemDto>>> ListAsync(
        ListItemsRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var (page, pageSize) = QueryHelpers.NormalizePaging(request.Page, request.PageSize);
        var status = QueryHelpers.ParseStatus(request.Status);
        var sortDirection = QueryHelpers.ParseSortDirection(request.SortDirection);

        var paged = await _items.GetPagedAsync(
            item => !item.IsDeleted
                    && (status == null || item.Status == status)
                    && (string.IsNullOrWhiteSpace(request.Search)
                        || item.Name.Contains(request.Search)
                        || (item.Description != null && item.Description.Contains(request.Search))),
            page,
            pageSize,
            request.SortBy,
            sortDirection,
            cancellationToken);

        var dto = PaginationResult<ItemDto>.Create(
            paged.Items.Select(x => x.ToDto()).ToList(),
            paged.Page,
            paged.PageSize,
            paged.TotalCount);

        return Result.Success(dto);
    }

    public async Task<Result<ItemDto>> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var cacheKey = $"{AppConstants.Cache.ItemPrefix}{id:N}";
        var cached = await _cache.GetAsync<ItemDto>(cacheKey, cancellationToken);
        if (cached is not null)
        {
            return Result.Success(cached);
        }

        var item = await _items.GetByIdAsync(id, cancellationToken);
        if (item is null || item.IsDeleted)
        {
            return Result.Failure<ItemDto>(Error.NotFound($"Item '{id}' was not found."));
        }

        var dto = item.ToDto();
        await _cache.SetAsync(cacheKey, dto, AppConstants.Cache.DefaultTtl, cancellationToken);
        return Result.Success(dto);
    }

    public async Task<Result<ItemDto>> CreateAsync(
        CreateItemRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var priority = QueryHelpers.ParsePriority(request.Priority);
        var item = Item.Create(request.Name, request.Description, priority);

        await _items.AddAsync(item, cancellationToken);
        await _items.SaveChangesAsync(cancellationToken);

        var dto = item.ToDto();

        // Redis cache example
        await _cache.SetAsync($"{AppConstants.Cache.ItemPrefix}{item.Id:N}", dto, AppConstants.Cache.DefaultTtl, cancellationToken);

        // Kafka publish example
        await _publisher.PublishAsync(AppConstants.Messaging.ItemCreatedTopic, dto, cancellationToken);

        // ElasticSearch indexing example
        await _search.IndexAsync(AppConstants.Search.ItemsIndex, item.Id.ToString("N"), dto, cancellationToken);

        _logger.LogInformation("Created item {ItemId}", item.Id);
        return Result.Success(dto);
    }

    public async Task<Result<ItemDto>> UpdateAsync(
        Guid id,
        UpdateItemRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var item = await _items.GetByIdAsync(id, cancellationToken);
        if (item is null || item.IsDeleted)
        {
            return Result.Failure<ItemDto>(Error.NotFound($"Item '{id}' was not found."));
        }

        if (!Enum.TryParse<Status>(request.Status, ignoreCase: true, out var status))
        {
            return Result.Failure<ItemDto>(Error.Validation("Invalid status value."));
        }

        var priority = QueryHelpers.ParsePriority(request.Priority, item.Priority);
        item.Update(request.Name, request.Description, status, priority);
        _items.Update(item);
        await _items.SaveChangesAsync(cancellationToken);

        var dto = item.ToDto();
        await _cache.SetAsync($"{AppConstants.Cache.ItemPrefix}{id:N}", dto, AppConstants.Cache.DefaultTtl, cancellationToken);
        await _publisher.PublishAsync(AppConstants.Messaging.ItemUpdatedTopic, dto, cancellationToken);
        await _search.IndexAsync(AppConstants.Search.ItemsIndex, id.ToString("N"), dto, cancellationToken);

        return Result.Success(dto);
    }

    public async Task<Result> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var item = await _items.GetByIdAsync(id, cancellationToken);
        if (item is null || item.IsDeleted)
        {
            return Result.Failure(Error.NotFound($"Item '{id}' was not found."));
        }

        item.SoftDelete();
        _items.Update(item);
        await _items.SaveChangesAsync(cancellationToken);

        await _cache.RemoveAsync($"{AppConstants.Cache.ItemPrefix}{id:N}", cancellationToken);
        await _publisher.PublishAsync(AppConstants.Messaging.ItemDeletedTopic, new { Id = id }, cancellationToken);
        await _search.DeleteAsync(AppConstants.Search.ItemsIndex, id.ToString("N"), cancellationToken);

        return Result.Success();
    }

    public async Task<Result<ItemDto>> UploadAttachmentAsync(
        Guid id,
        string fileName,
        string contentType,
        Stream content,
        CancellationToken cancellationToken = default)
    {
        var item = await _items.GetByIdAsync(id, cancellationToken);
        if (item is null || item.IsDeleted)
        {
            return Result.Failure<ItemDto>(Error.NotFound($"Item '{id}' was not found."));
        }

        if (content.CanSeek && content.Length > AppConstants.Storage.MaxUploadBytes)
        {
            throw new ApplicationException("File exceeds maximum allowed size.", "file.too_large");
        }

        var objectKey = FileHelpers.BuildObjectKey("items", fileName);
        await _storage.UploadAsync(objectKey, content, contentType, cancellationToken);

        item.SetAttachment(objectKey);
        _items.Update(item);
        await _items.SaveChangesAsync(cancellationToken);

        var dto = item.ToDto();
        await _cache.SetAsync($"{AppConstants.Cache.ItemPrefix}{id:N}", dto, AppConstants.Cache.DefaultTtl, cancellationToken);
        return Result.Success(dto);
    }
}
