using System.Globalization;
using EnemyRandomizer.Core.Configuration;

namespace EnemyRandomizer.Core.Settings;

/// <summary>
/// The shareable text config of v0.4.x: option values ordered by <see cref="OptionDefinition.LegacyIndex"/>,
/// separated by <c>/-/</c>, followed by the seed in triple quotes (legacy <c>randomizer.py:1507-1635</c>).
/// Example: <c>1/-/2/-/0/-/.../-/1/-/'''my seed'''</c>. Strings from v0.4.x stay compatible.
/// </summary>
public static class LegacyConfigString
{
    private const string Separator = "/-/";
    private const string SeedQuote = "'''";

    public static string Format(RandomizerSettings settings, RandomizerOptionSettings options)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(options);

        var values = options.Options
            .OrderBy(o => o.LegacyIndex)
            .Select(o => settings[o.Key].ToString(CultureInfo.InvariantCulture));

        return string.Join(Separator, values.Append(SeedQuote + settings.Seed + SeedQuote));
    }

    /// <summary>
    /// Parses <paramref name="text"/>. The enemy config is not part of the text and is taken from
    /// <paramref name="enemyConfig"/>. On failure <paramref name="error"/> says what is wrong.
    /// </summary>
    public static bool TryParse(
        string text,
        RandomizerOptionSettings options,
        string enemyConfig,
        out RandomizerSettings? settings,
        out string error)
    {
        ArgumentNullException.ThrowIfNull(options);
        settings = null;

        var parts = (text ?? "").Trim().Split(Separator);
        var expected = options.Options.Count + 1;
        if (parts.Length != expected)
        {
            error = $"expected {expected} fields separated by '{Separator}', found {parts.Length}.";
            return false;
        }

        var values = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var option in options.Options)
        {
            var part = parts[option.LegacyIndex];
            if (!int.TryParse(part, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value)
                || !option.IsValidValue(value))
            {
                error = $"'{part}' is not a valid value for '{option.Title.TrimEnd(':')}'.";
                return false;
            }

            values[option.Key] = value;
        }

        var seed = parts[^1].Replace(SeedQuote, "", StringComparison.Ordinal);
        settings = new RandomizerSettings(values, seed, enemyConfig);
        error = "";
        return true;
    }
}
