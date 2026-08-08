namespace BaseWebApi.Application.Abstractions.Search;

public interface ISearchService
{
    Task IndexAsync<T>(string indexName, string documentId, T document, CancellationToken cancellationToken = default)
        where T : class;

    Task DeleteAsync(string indexName, string documentId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<T>> SearchAsync<T>(string indexName, string query, CancellationToken cancellationToken = default)
        where T : class;
}
