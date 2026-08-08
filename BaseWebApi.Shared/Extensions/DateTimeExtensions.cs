namespace BaseWebApi.Shared.Extensions;

public static class DateTimeExtensions
{
    public static DateTime ToUtc(this DateTime value) =>
        value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
        };

    public static DateTimeOffset StartOfDay(this DateTimeOffset value) =>
        new(value.Year, value.Month, value.Day, 0, 0, 0, value.Offset);

    public static DateTimeOffset EndOfDay(this DateTimeOffset value) =>
        value.StartOfDay().AddDays(1).AddTicks(-1);

    public static bool IsBetween(this DateTimeOffset value, DateTimeOffset start, DateTimeOffset end) =>
        value >= start && value <= end;

    public static string ToIso8601(this DateTimeOffset value) =>
        value.ToUniversalTime().ToString("O");
}
