using System.Collections.Concurrent;
using System.Text.Json;
using BaseWebApi.Application.Abstractions.Caching;
using BaseWebApi.Application.Abstractions.Messaging;
using BaseWebApi.Application.Abstractions.Search;
using BaseWebApi.Application.Abstractions.Storage;
using Microsoft.Extensions.Logging;

namespace BaseWebApi.Infrastructure.Fallbacks;

/// <summary>In-process cache used when Redis is unavailable.</summary>
public sealed class MemoryCacheService : ICacheService
{
    private readonly ConcurrentDictionary<string, (byte[] Payload, DateTimeOffset? Expires)> _store = new();
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        if (!_store.TryGetValue(key, out var entry))
        {
            return Task.FromResult<T?>(default);
        }

        if (entry.Expires is not null && entry.Expires <= DateTimeOffset.UtcNow)
        {
            _store.TryRemove(key, out _);
            return Task.FromResult<T?>(default);
        }

        return Task.FromResult(JsonSerializer.Deserialize<T>(entry.Payload, JsonOptions));
    }

    public Task SetAsync<T>(string key, T value, TimeSpan? ttl = null, CancellationToken cancellationToken = default)
    {
        var bytes = JsonSerializer.SerializeToUtf8Bytes(value, JsonOptions);
        DateTimeOffset? expires = ttl.HasValue ? DateTimeOffset.UtcNow.Add(ttl.Value) : null;
        _store[key] = (bytes, expires);
        return Task.CompletedTask;
    }

    public Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        _store.TryRemove(key, out _);
        return Task.CompletedTask;
    }
}

/// <summary>Logs events instead of publishing to Kafka.</summary>
public sealed class NoOpEventPublisher : IEventPublisher
{
    private readonly ILogger<NoOpEventPublisher> _logger;

    public NoOpEventPublisher(ILogger<NoOpEventPublisher> logger) => _logger = logger;

    public Task PublishAsync<T>(string topic, T message, CancellationToken cancellationToken = default)
        where T : class
    {
        _logger.LogDebug("Local fallback: skipped Kafka publish to {Topic} ({Type})", topic, typeof(T).Name);
        return Task.CompletedTask;
    }
}

/// <summary>Keeps uploads in memory / temp files when MinIO is unavailable.</summary>
public sealed class InMemoryObjectStorage : IObjectStorage
{
    private readonly ConcurrentDictionary<string, (string ContentType, byte[] Content)> _objects = new();
    private readonly ILogger<InMemoryObjectStorage> _logger;

    public InMemoryObjectStorage(ILogger<InMemoryObjectStorage> logger) => _logger = logger;

    public async Task<string> UploadAsync(
        string objectKey,
        Stream content,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        using var ms = new MemoryStream();
        await content.CopyToAsync(ms, cancellationToken);
        _objects[objectKey] = (contentType, ms.ToArray());
        _logger.LogDebug("Local fallback: stored object {ObjectKey} ({Bytes} bytes)", objectKey, ms.Length);
        return objectKey;
    }

    public Task<Stream> DownloadAsync(string objectKey, CancellationToken cancellationToken = default)
    {
        if (!_objects.TryGetValue(objectKey, out var item))
        {
            throw new FileNotFoundException($"Object '{objectKey}' was not found in local storage.");
        }

        return Task.FromResult<Stream>(new MemoryStream(item.Content));
    }

    public Task DeleteAsync(string objectKey, CancellationToken cancellationToken = default)
    {
        _objects.TryRemove(objectKey, out _);
        return Task.CompletedTask;
    }

    public Task<string> GetPresignedUrlAsync(
        string objectKey,
        TimeSpan expiry,
        CancellationToken cancellationToken = default) =>
        Task.FromResult($"memory://{objectKey}?expiry={expiry.TotalSeconds}");
}

/// <summary>No-op search indexer used when Elasticsearch is unavailable.</summary>
public sealed class NoOpSearchService : ISearchService
{
    private readonly ILogger<NoOpSearchService> _logger;

    public NoOpSearchService(ILogger<NoOpSearchService> logger) => _logger = logger;

    public Task IndexAsync<T>(string indexName, string documentId, T document, CancellationToken cancellationToken = default)
        where T : class
    {
        _logger.LogDebug("Local fallback: skipped Elastic index {Index}/{Id}", indexName, documentId);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(string indexName, string documentId, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Local fallback: skipped Elastic delete {Index}/{Id}", indexName, documentId);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<T>> SearchAsync<T>(string indexName, string query, CancellationToken cancellationToken = default)
        where T : class
    {
        _logger.LogDebug("Local fallback: Elastic search on {Index} returned empty", indexName);
        return Task.FromResult<IReadOnlyList<T>>([]);
    }
}
