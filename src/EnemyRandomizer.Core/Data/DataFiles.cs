using System.Globalization;

namespace EnemyRandomizer.Core.Data;

/// <summary>
/// Parsers for the tab separated data files shipped in <c>enemyRandomizerData</c>
/// (legacy <c>loadFiles</c>, <c>ai_data.py</c>, <c>randomizer_rng.py:1406-1420</c>).
/// </summary>
public static class DataFiles
{
    /// <summary>Column positions of the enemy catalog (legacy <c>NewCol</c>).</summary>
    internal static class Catalog
    {
        public const int Id = 0;
        public const int Name = 1;
        public const int Type = 2;
        public const int Ignored = 3;
        public const int Size = 4;
        public const int Difficulty = 5;
        public const int Locations = 6;
        public const int Ai = 7;
        public const int Param = 8;
        public const int IdleAnimations = 9;
        public const int Comment = 10;
        public const int MinColumns = 10;
    }

    /// <summary>
    /// Reads the enemy catalog (header skipped). Rows whose AI and param lists differ in length are skipped
    /// and reported in <paramref name="skipped"/>, like v0.4.x; <see cref="EnemyDefinition.Index"/> counts kept rows.
    /// </summary>
    public static IReadOnlyList<EnemyDefinition> ParseCatalog(IEnumerable<string> lines, int maxDifficulty, out IReadOnlyList<string> skipped)
    {
        ArgumentNullException.ThrowIfNull(lines);
        var result = new List<EnemyDefinition>();
        var bad = new List<string>();
        foreach (var (parts, row) in Rows(lines))
        {
            if (parts.Length < Catalog.MinColumns)
            {
                throw new FormatException($"Enemy catalog row {row} has {parts.Length} columns, expected at least {Catalog.MinColumns}.");
            }

            var ais = Ints(parts[Catalog.Ai], row);
            var param = Ints(parts[Catalog.Param], row);
            if (ais.Count != param.Count)
            {
                bad.Add(parts[Catalog.Id]);
                continue;
            }

            var size = Int(parts[Catalog.Size], row);
            var difficulty = Int(parts[Catalog.Difficulty], row);
            if (size is < 0 or >= EnemyPool.SizeClasses || difficulty < 0 || difficulty > maxDifficulty)
            {
                throw new FormatException($"Enemy catalog row {row} ({parts[Catalog.Id]}): size {size} or difficulty {difficulty} out of range.");
            }

            result.Add(new EnemyDefinition(
                result.Count,
                parts[Catalog.Id],
                parts[Catalog.Name],
                (EnemyType)Int(parts[Catalog.Type], row),
                parts[Catalog.Ignored],
                size,
                difficulty,
                Split(parts[Catalog.Locations]),
                ais,
                param,
                Ints(parts[Catalog.IdleAnimations], row),
                parts.Length > Catalog.Comment ? parts[Catalog.Comment] : ""));
        }

        skipped = bad;
        return result;
    }

    /// <summary>Reads <c>valid_replacements.txt</c> (header skipped): <c>ID Name Type Difficulty</c>.</summary>
    public static IReadOnlyList<ReplacementTarget> ParseReplacementTargets(IEnumerable<string> lines) =>
        Rows(lines).Select(r => new ReplacementTarget(
            r.Parts[0], r.Parts[1], (EnemyType)Int(r.Parts[2], r.Row), Int(r.Parts[3], r.Row))).ToList();

    /// <summary>Reads an <c>original_enemies_ref</c> file (no header): <c>partName&lt;TAB&gt;size</c>.</summary>
    public static IReadOnlyList<OriginalEnemy> ParseOriginalEnemies(IEnumerable<string> lines)
    {
        ArgumentNullException.ThrowIfNull(lines);
        return lines.Select((line, i) => (Parts: line.Trim().Split('\t'), Row: i + 1))
                    .Where(r => r.Parts[0].Length > 0)
                    .Select(r => new OriginalEnemy(r.Parts[0], r.Parts.Length > 1 ? Int(r.Parts[1], r.Row) : 0))
                    .ToList();
    }

    /// <summary>
    /// Reads <c>airef.csv</c> (tab separated, no header): ai, description, files name, battle script, logic script,
    /// <c>;</c>-separated gnl functions, <c>|</c>-separated info entries. Info entries are reversed like v0.4.x,
    /// because they are inserted at the front of the luainfo one by one.
    /// </summary>
    public static IReadOnlyList<AiReference> ParseAiReferences(IEnumerable<string> lines)
    {
        ArgumentNullException.ThrowIfNull(lines);
        var result = new List<AiReference>();
        foreach (var (line, index) in lines.Select((l, i) => (l.Trim(), i + 1)).Where(x => x.Item1.Length > 0))
        {
            var parts = line.Split('\t');
            if (parts.Length < 7)
            {
                throw new FormatException($"airef.csv row {index} has {parts.Length} columns, expected 7.");
            }

            var info = parts[6].Split('|').Select(entry =>
            {
                var p = entry.Split(';');
                if (p.Length < 7)
                {
                    throw new FormatException($"airef.csv row {index}: info entry '{entry}' needs 7 values.");
                }

                return new AiInfoEntry(Int(p[0], index), p[1], p[2], Int(p[3], index), Int(p[4], index), Int(p[5], index), Int(p[6], index));
            }).Reverse().ToList();

            result.Add(new AiReference(Int(parts[0], index), parts[1], parts[2], parts[3], parts[4], parts[5].Split(';'), info));
        }

        return result;
    }

    private static IEnumerable<(string[] Parts, int Row)> Rows(IEnumerable<string> lines)
    {
        ArgumentNullException.ThrowIfNull(lines);
        return lines.Skip(1)
                    .Select((line, i) => (Line: line.Trim(), Row: i + 1))
                    .Where(x => x.Line.Length > 0)
                    .Select(x => (x.Line.Split('\t'), x.Row));
    }

    private static List<string> Split(string value) =>
        [.. value.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)];

    private static List<int> Ints(string value, int row) =>
        [.. value.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Select(v => Int(v, row))];

    private static int Int(string value, int row) =>
        int.TryParse(value.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var result)
            ? result
            : throw new FormatException($"Row {row}: '{value}' is not a number.");
}
