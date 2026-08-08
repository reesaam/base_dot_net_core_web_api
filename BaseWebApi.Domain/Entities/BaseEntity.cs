namespace BaseWebApi.Domain.Entities;

/// <summary>
/// Base entity with strongly-typed identity and audit fields.
/// </summary>
public abstract class BaseEntity<TId> where TId : notnull
{
    public TId Id { get; protected set; } = default!;

    public DateTimeOffset CreatedAt { get; protected set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset? UpdatedAt { get; protected set; }

    public string? CreatedBy { get; protected set; }

    public string? UpdatedBy { get; protected set; }

    public bool IsDeleted { get; protected set; }

    public void MarkCreated(string? userId = null)
    {
        CreatedAt = DateTimeOffset.UtcNow;
        CreatedBy = userId;
        UpdatedAt = null;
        UpdatedBy = null;
    }

    public void MarkUpdated(string? userId = null)
    {
        UpdatedAt = DateTimeOffset.UtcNow;
        UpdatedBy = userId;
    }

    public void SoftDelete(string? userId = null)
    {
        IsDeleted = true;
        MarkUpdated(userId);
    }
}
