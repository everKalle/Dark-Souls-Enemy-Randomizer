using System.Globalization;

namespace EnemyRandomizer.Core.Randomization;

/// <summary>One creature of a saved layout: where it stands and what it became.</summary>
public sealed record LayoutRow(
    float X, float Y, float Z,
    float RotX, float RotY, float RotZ,
    int Model, int Ai, int Param, int EntityId, int AnimId);

/// <summary>The saved creatures of one map. <see cref="NextEventEntityId"/> is the <c>levID</c> line.</summary>
public sealed record MapLayout(int NextEventEntityId, IReadOnlyDictionary<string, LayoutRow> Rows);

/// <summary>
/// A parsed enemy layout reference file (<c>refs/enemy-layout-*.txt</c>, legacy <c>CopyFromReference</c> parser,
/// <c>randomizer_rng.py:1803-1856</c>). Pure text parsing; the format is written by the infrastructure.
/// </summary>
public sealed record LayoutReference(
    string RandomizerVersion,
    bool Remastered,
    int BossSouls,
    int PinwheelChaos,
    int RoamingBossRespawn,
    IReadOnlyDictionary<string, MapLayout> Maps)
{
    private const string VersionSection = "version";
    private const string SettingsSection = "settings";
    private const int RowFields = 12;

    /// <summary>Parses the file lines; throws <see cref="InvalidDataException"/> with a user readable message.</summary>
    public static LayoutReference Parse(IEnumerable<string> lines)
    {
        ArgumentNullException.ThrowIfNull(lines);
        var settings = new Dictionary<string, int>(StringComparer.Ordinal);
        var version = new Dictionary<string, string>(StringComparer.Ordinal);
        var levIds = new Dictionary<string, int>(StringComparer.Ordinal);
        var rows = new Dictionary<string, Dictionary<string, LayoutRow>>(StringComparer.Ordinal);
        var section = "";
        var lineNumber = 0;

        foreach (var raw in lines)
        {
            var line = raw.Trim();
            if (line.Length == 0)
            {
                lineNumber++;
                continue;
            }

            if (line.StartsWith("section ", StringComparison.Ordinal))
            {
                section = line[8..].Trim();
                if (section is not (VersionSection or SettingsSection))
                {
                    rows[section] = new Dictionary<string, LayoutRow>(StringComparer.Ordinal);
                }
            }
            else if (line.StartsWith("levID ", StringComparison.Ordinal))
            {
                levIds[section] = ParseInt(line[6..], lineNumber, line);
            }
            else if (section == SettingsSection)
            {
                var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                settings[parts[0]] = parts.Length > 1 ? ParseInt(parts[1], lineNumber, line) : throw Malformed(lineNumber, line);
            }
            else if (section == VersionSection)
            {
                var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                version[parts[0]] = parts.Length > 1 ? parts[1] : throw Malformed(lineNumber, line);
            }
            else if (rows.TryGetValue(section, out var map))
            {
                var parts = line.Split('\t');
                if (parts.Length != RowFields)
                {
                    throw Malformed(lineNumber, line);
                }

                map[parts[0]] = ParseRow(parts, lineNumber, line);
            }
            else
            {
                throw Malformed(lineNumber, line);
            }

            lineNumber++;
        }

        if (!settings.TryGetValue("bossSouls", out var bossSouls))
        {
            throw new InvalidDataException("Setting 'bossSouls' missing from the reference file.");
        }

        if (!settings.TryGetValue("pinwheelChaos", out var pinwheel))
        {
            throw new InvalidDataException("Setting 'pinwheelChaos' missing from the reference file.");
        }

        if (!settings.TryGetValue("roamingBossRespawn", out var roaming))
        {
            throw new InvalidDataException("Setting 'roamingBossRespawn' missing from the reference file.");
        }

        if (!version.TryGetValue("randomizerVersion", out var randomizerVersion))
        {
            throw new InvalidDataException("Randomizer version info missing from the reference file.");
        }

        if (!version.TryGetValue("mode", out var mode))
        {
            throw new InvalidDataException("Randomizer mode info missing from the reference file.");
        }

        if (mode is not ("Remastered" or "PTDE"))
        {
            throw new InvalidDataException($"Invalid randomizer mode in reference file: '{mode}'. Expected 'PTDE' or 'Remastered'");
        }

        var maps = rows.ToDictionary(
            m => m.Key,
            m => new MapLayout(levIds.GetValueOrDefault(m.Key), m.Value),
            StringComparer.Ordinal);
        return new LayoutReference(randomizerVersion, mode == "Remastered", bossSouls, pinwheel, roaming, maps);
    }

    private static LayoutRow ParseRow(string[] p, int lineNumber, string line)
    {
        try
        {
            return new LayoutRow(
                Float(p[1]), Float(p[2]), Float(p[3]), Float(p[4]), Float(p[5]), Float(p[6]),
                Int(p[7]), Int(p[8]), Int(p[9]), Int(p[10]), Int(p[11]));
        }
        catch (FormatException)
        {
            throw Malformed(lineNumber, line);
        }
        catch (OverflowException)
        {
            throw Malformed(lineNumber, line);
        }

        static float Float(string s) => float.Parse(s, NumberStyles.Float, CultureInfo.InvariantCulture);
        static int Int(string s) => int.Parse(s, NumberStyles.Integer, CultureInfo.InvariantCulture);
    }

    private static int ParseInt(string text, int lineNumber, string line) =>
        int.TryParse(text.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var value)
            ? value
            : throw Malformed(lineNumber, line);

    private static InvalidDataException Malformed(int lineNumber, string line) =>
        new($"Malformed line in reference file: {lineNumber} - {line}");
}
