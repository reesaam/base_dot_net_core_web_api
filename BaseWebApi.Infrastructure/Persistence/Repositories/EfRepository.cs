using System.Linq.Expressions;
using BaseWebApi.Application.Abstractions.Persistence;
using BaseWebApi.Domain.Enums;
using BaseWebApi.Shared.Results;
using Microsoft.EntityFrameworkCore;

namespace BaseWebApi.Infrastructure.Persistence.Repositories;

public class EfRepository<TEntity> : IEfRepository<TEntity>
    where TEntity : class
{
    protected readonly AppDbContext DbContext;
    protected readonly DbSet<TEntity> DbSet;

    public EfRepository(AppDbContext dbContext)
    {
        DbContext = dbContext;
        DbSet = dbContext.Set<TEntity>();
    }

    public virtual async Task<TEntity?> GetByIdAsync(object id, CancellationToken cancellationToken = default) =>
        await DbSet.FindAsync([id], cancellationToken);

    public virtual async Task<IReadOnlyList<TEntity>> ListAsync(CancellationToken cancellationToken = default) =>
        await DbSet.AsNoTracking().ToListAsync(cancellationToken);

    public virtual async Task AddAsync(TEntity entity, CancellationToken cancellationToken = default) =>
        await DbSet.AddAsync(entity, cancellationToken);

    public virtual void Update(TEntity entity) => DbSet.Update(entity);

    public virtual void Remove(TEntity entity) => DbSet.Remove(entity);

    public virtual Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
        DbContext.SaveChangesAsync(cancellationToken);

    public virtual IQueryable<TEntity> Query(bool asNoTracking = true) =>
        asNoTracking ? DbSet.AsNoTracking() : DbSet.AsQueryable();

    public virtual Task<bool> AnyAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default) =>
        DbSet.AnyAsync(predicate, cancellationToken);

    public virtual async Task<PaginationResult<TEntity>> GetPagedAsync(
        Expression<Func<TEntity, bool>>? predicate,
        int page,
        int pageSize,
        string? sortBy,
        SortDirection sortDirection,
        CancellationToken cancellationToken = default)
    {
        IQueryable<TEntity> query = DbSet.AsNoTracking();

        if (predicate is not null)
        {
            query = query.Where(predicate);
        }

        query = ApplySorting(query, sortBy, sortDirection);

        var total = await query.LongCountAsync(cancellationToken);
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return PaginationResult<TEntity>.Create(items, page, pageSize, total);
    }

    protected virtual IQueryable<TEntity> ApplySorting(
        IQueryable<TEntity> query,
        string? sortBy,
        SortDirection sortDirection)
    {
        if (string.IsNullOrWhiteSpace(sortBy))
        {
            return query;
        }

        // Generic reflection-based sort; specialize per entity when performance matters.
        var parameter = Expression.Parameter(typeof(TEntity), "x");
        var property = typeof(TEntity).GetProperty(
            sortBy,
            System.Reflection.BindingFlags.IgnoreCase
            | System.Reflection.BindingFlags.Public
            | System.Reflection.BindingFlags.Instance);

        if (property is null)
        {
            return query;
        }

        var propertyAccess = Expression.Property(parameter, property);
        var conversion = Expression.Convert(propertyAccess, typeof(object));
        var lambda = Expression.Lambda<Func<TEntity, object>>(conversion, parameter);

        return sortDirection == SortDirection.Desc
            ? query.OrderByDescending(lambda)
            : query.OrderBy(lambda);
    }
}
