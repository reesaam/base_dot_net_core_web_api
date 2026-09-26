using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace BaseWebApi.Shared.Extensions;

public static class StringExtensions
{
    private static readonly Regex CollapseDashesRegex = new("-{2,}", RegexOptions.Compiled);

    public static bool IsNullOrWhiteSpace(this string? value) =>
        string.IsNullOrWhiteSpace(value);

    public static string Truncate(this string value, int maxLength, string suffix = "…")
    {
        if (string.IsNullOrEmpty(value) || value.Length <= maxLength) return value;
        if (maxLength <= suffix.Length) return value[..maxLength];
        return value[..(maxLength - suffix.Length)] + suffix;
    }

    public static string ToSlug(this string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return string.Empty;

        var normalized = value.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(normalized.Length);

        foreach (var ch in normalized)
        {
            var category = CharUnicodeInfo.GetUnicodeCategory(ch);
            if (category == UnicodeCategory.NonSpacingMark) continue;
            builder.Append(char.IsLetterOrDigit(ch) ? ch : '-');
        }

        return CollapseDashesRegex.Replace(builder.ToString().Trim('-'), "-");
    }

    public static string? NullIfWhiteSpace(this string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
