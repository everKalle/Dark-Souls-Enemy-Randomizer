namespace EnemyRandomizer.Core.EnemyConfigs;

/// <summary>One enemy of an enemy config: can it be used as a replacement?</summary>
public sealed record EnemyConfigEntry(string Id, string Name, bool Enabled, string Comment);

/// <summary>
/// Tab separated enemy catalog format of <c>replacement_ref/valid_new.txt</c> and <c>customConfigs/*.txt</c>.
/// Columns: <c>ID Name Type IsIgnored Size Difficulty Locations ValidAI Param ValidIdleAnimIDs [Comments]</c>.
/// <c>IsIgnored</c>: 0 = enabled, 1 = disabled, 2 = enabled on Remastered only (Artorias).
/// Legacy <c>randomizer.py:1208-1248, 1444-1479</c>.
/// </summary>
public static class EnemyConfigFile
{
    private const int IdColumn = Data.DataFiles.Catalog.Id;
    private const int NameColumn = Data.DataFiles.Catalog.Name;
    private const int IgnoredColumn = Data.DataFiles.Catalog.Ignored;
    private const int CommentColumn = Data.DataFiles.Catalog.Comment;
    private const int MinColumns = Data.DataFiles.Catalog.MinColumns;

    /// <summary>Reads the enemy rows (header skipped).</summary>
    public static IReadOnlyList<EnemyConfigEntry> Parse(IEnumerable<string> lines, GameEdition edition)
    {
        ArgumentNullException.ThrowIfNull(lines);
        return Rows(lines).Select((parts, index) =>
        {
            if (parts.Length < MinColumns)
            {
                throw new FormatException($"Enemy config row {index + 1} has {parts.Length} columns, expected at least {MinColumns}.");
            }

            return new EnemyConfigEntry(
                parts[IdColumn],
                parts[NameColumn],
                Data.EnemyDefinition.IsEnabled(parts[IgnoredColumn], edition),
                parts.Length > CommentColumn ? parts[CommentColumn] : "");
        }).ToList();
    }

    /// <summary>
    /// Builds a custom config from the catalog <paramref name="templateLines"/> with the given enabled flags
    /// (same output as v0.4.x: header, then one row per enemy, no trailing newline).
    /// </summary>
    public static string Write(IReadOnlyList<string> templateLines, IReadOnlyList<bool> enabled)
    {
        ArgumentNullException.ThrowIfNull(templateLines);
        ArgumentNullException.ThrowIfNull(enabled);

        var rows = Rows(templateLines).ToList();
        if (rows.Count != enabled.Count)
        {
            throw new ArgumentException($"Expected {rows.Count} enabled flags, got {enabled.Count}.", nameof(enabled));
        }

        var output = new List<string> { templateLines[0].Trim() };
        for (var i = 0; i < rows.Count; i++)
        {
            var parts = rows[i];
            var columns = new List<string>(parts[..IgnoredColumn]) { enabled[i] ? "0" : "1" };
            columns.AddRange(parts[(IgnoredColumn + 1)..MinColumns]);
            columns.Add(parts.Length > CommentColumn ? parts[CommentColumn] : "");
            output.Add(string.Join('\t', columns));
        }

        return string.Join('\n', output);
    }

    private static IEnumerable<string[]> Rows(IEnumerable<string> lines) =>
        lines.Skip(1)
             .Where(l => !string.IsNullOrWhiteSpace(l))
             .Select(l => l.Trim().Split('\t'));
}
