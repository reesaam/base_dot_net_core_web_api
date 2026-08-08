namespace BaseWebApi.Domain.ValueObjects;

/// <summary>
/// Sample money value object for finance-style modules.
/// TODO: Add currency conversion / rounding policies.
/// </summary>
public sealed record Money(decimal Amount, string Currency) : ValueObject
{
    public static Money Zero(string currency = "USD") => new(0m, currency);

    public Money Add(Money other)
    {
        if (!Currency.Equals(other.Currency, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Cannot add amounts with different currencies.");
        }

        return this with { Amount = Amount + other.Amount };
    }
}
