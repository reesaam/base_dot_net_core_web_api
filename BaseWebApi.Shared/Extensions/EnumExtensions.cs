namespace BaseWebApi.Shared.Extensions;

public static class EnumExtensions
{
    public static string ToDisplayName(this Enum value) =>
        value.ToString().Replace('_', ' ');

    public static bool TryParseFlexible<TEnum>(string? input, out TEnum result)
        where TEnum : struct, Enum
    {
        result = default;
        if (string.IsNullOrWhiteSpace(input)) return false;

        return Enum.TryParse(input.Replace(' ', '_'), ignoreCase: true, out result) ||
               Enum.TryParse(input, ignoreCase: true, out result);
    }

    public static IReadOnlyList<TEnum> GetValues<TEnum>()
        where TEnum : struct, Enum =>
        Enum.GetValues<TEnum>();
}