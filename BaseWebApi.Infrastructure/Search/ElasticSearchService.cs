using BaseWebApi.Application.Abstractions.Search;
using BaseWebApi.Infrastructure.Configuration;
using Microsoft.Extensions.Logging;
using Nest;

namespace BaseWebApi.Infrastructure.Search;

public sealed class ElasticSearchService : ISearchService
{
    private readonly IElasticClient _client;
    private readonly ILogger<ElasticSearchService> _logger;

    public ElasticSearchService(IElasticClient client, ILogger<ElasticSearchService> logger)
    {
        _client = client;
        _logger = logger;
    }

    public async Task IndexAsync<T>(
        string indexName,
        string documentId,
        T document,
        CancellationToken cancellationToken = default)
        where T : class
    {
        var response = await _client.IndexAsync(
            document,
            idx => idx.Index(indexName).Id(documentId),
            cancellationToken);

        if (!response.IsValid)
        {
            _logger.LogWarning(
                "Elasticsearch index failed for {Index}/{Id}: {Error}",
                indexName,
                documentId,
                response.ServerError?.ToString() ?? response.OriginalException?.Message);
        }
    }

    public async Task DeleteAsync(
        string indexName,
        string documentId,
        CancellationToken cancellationToken = default)
    {
        var response = await _client.DeleteAsync<object>(
            documentId,
            d => d.Index(indexName),
            cancellationToken);

        if (!response.IsValid && response.Result != Result.NotFound)
        {
            _logger.LogWarning(
                "Elasticsearch delete failed for {Index}/{Id}",
                indexName,
                documentId);
        }
    }

    public async Task<IReadOnlyList<T>> SearchAsync<T>(
        string indexName,
        string query,
        CancellationToken cancellationToken = default)
        where T : class
    {
        var response = await _client.SearchAsync<T>(
            s => s.Index(indexName).Query(q => q.QueryString(qs => qs.Query(query))),
            cancellationToken);

        if (!response.IsValid)
        {
            _logger.LogWarning("Elasticsearch query failed on {Index}", indexName);
            return [];
        }

        return response.Documents.ToList();
    }

    public static IElasticClient CreateClient(ElasticsearchOptions options)
    {
        var settings = new ConnectionSettings(new Uri(options.Url))
            .DefaultIndex(options.DefaultIndex)
            .PrettyJson();

        if (!string.IsNullOrWhiteSpace(options.Username))
        {
            settings = settings.BasicAuthentication(options.Username, options.Password ?? string.Empty);
        }

        // Soft-fail during local boot when Elastic is unavailable.
        settings = settings.ThrowExceptions(false);
        return new ElasticClient(settings);
    }
}
