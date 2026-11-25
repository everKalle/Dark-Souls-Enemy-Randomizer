namespace EnemyRandomizer.Core.Configuration;

/// <summary>Small reusable checks used by the settings validators.</summary>
internal static class ValidationHelpers
{
    public static IEnumerable<string> Duplicates<T>(IEnumerable<T> items, string what)
        where T : notnull =>
        items.GroupBy(i => i)
             .Where(g => g.Count() > 1)
             .Select(g => $"Duplicate {what} '{g.Key}'.");

    public static IEnumerable<string> RequireText(string? value, string what)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            yield return $"{what} must not be empty.";
        }
    }

    public static IEnumerable<string> RequireAbsoluteUrl(string? value, string what)
    {
        if (!Uri.TryCreate(value, UriKind.Absolute, out _))
        {
            yield return $"{what} must be an absolute URL.";
        }
    }

    public static IEnumerable<string> RequireRange(int value, int min, int max, string what)
    {
        if (value < min || value > max)
        {
            yield return $"{what} must be between {min} and {max} (was {value}).";
        }
    }
}
