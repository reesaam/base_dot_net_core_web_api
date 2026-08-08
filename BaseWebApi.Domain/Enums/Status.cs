namespace BaseWebApi.Domain.Enums;

/// <summary>
/// Generic lifecycle status for any enterprise entity.
/// TODO: Specialize per bounded context when needed (e.g. InvoiceStatus).
/// </summary>
public enum Status
{
    Draft = 0,
    Active = 1,
    Inactive = 2,
    Archived = 3,
    Deleted = 4
}
