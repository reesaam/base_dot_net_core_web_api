using System.Linq.Expressions;
using BaseWebApi.Domain.Enums;
using BaseWebApi.Shared.Results;

namespace BaseWebApi.Application.Abstractions.Persistence;

/// <summary>
/// Generic repository contract independent of EF Core.
/// </summary>
public interface IRepository<TEntity> where TEntity : class
{
    Task<TEntity?> GetByIdAsync(object id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TEntity>> ListAsync(CancellationToken cancellationToken = default);

    Task AddAsync(TEntity entity, CancellationToken cancellationToken = default);

    void Update(TEntity entity);

    void Remove(TEntity entity);

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// EF-oriented repository with query helpers for filtering/sorting/paging.
/// </summary>
public interface IEfRepository<TEntity> : IRepository<TEntity> where TEntity : class
{
    IQueryable<TEntity> Query(bool asNoTracking = true);

    Task<bool> AnyAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default);

    Task<PaginationResult<TEntity>> GetPagedAsync(
        Expression<Func<TEntity, bool>>? predicate,
        int page,
        int pageSize,
        string? sortBy,
        SortDirection sortDirection,
        CancellationToken cancellationToken = default);
}
