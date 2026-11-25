namespace EnemyRandomizer.Core.Configuration;

/// <summary>
/// Game install layout, maps and per-edition values (<c>config/game-editions.json</c>, section <c>GameData</c>).
/// Replaces the hardcoded tables in legacy <c>randomizer_rng.py:50-75, 261-262</c> and <c>check_exe.py</c>.
/// </summary>
public sealed class GameDataSettings : IValidatableSettings
{
    public static string SectionName => "GameData";

    /// <summary>Suffix appended to game files on editions that use DCX compression.</summary>
    public string CompressedSuffix { get; set; } = "";

    public GameFolderSettings Folders { get; set; } = new();

    /// <summary>Index of NpcParam inside GameParam.parambnd.</summary>
    public int NpcParamIndex { get; set; }

    public string MimicModelId { get; set; } = "";

    /// <summary>Effect bundles merged into CommonEffects (<c>sfx/FRPG_SfxBnd_{name}.ffxbnd</c>).</summary>
    public List<string> FfxFiles { get; set; } = [];

    /// <summary>All maps, in the order the legacy randomizer processed them.</summary>
    public List<MapDefinition> Maps { get; set; } = [];

    public Dictionary<GameEdition, GameEditionDefinition> Editions { get; set; } = [];

    public IEnumerable<string> Validate()
    {
        var errors = new List<string>();
        errors.AddRange(ValidationHelpers.RequireText(CompressedSuffix, "GameData.CompressedSuffix"));
        errors.AddRange(ValidationHelpers.RequireText(MimicModelId, "GameData.MimicModelId"));
        errors.AddRange(Folders.Validate());
        errors.AddRange(ValidationHelpers.Duplicates(FfxFiles, "FFX file"));
        errors.AddRange(ValidationHelpers.Duplicates(Maps.Select(m => m.Id), "map id"));

        if (FfxFiles.Count == 0)
        {
            errors.Add("GameData.FfxFiles must not be empty.");
        }

        if (Maps.Count == 0)
        {
            errors.Add("GameData.Maps must not be empty.");
        }

        foreach (var edition in Enum.GetValues<GameEdition>())
        {
            if (!Editions.TryGetValue(edition, out var definition))
            {
                errors.Add($"GameData.Editions is missing '{edition}'.");
                continue;
            }

            errors.AddRange(definition.Validate(edition));
        }

        foreach (var map in Maps)
        {
            errors.AddRange(map.Validate());
        }

        return errors;
    }
}

/// <summary>Game folders, relative to the game install folder.</summary>
public sealed class GameFolderSettings
{
    public string MapStudio { get; set; } = "";
    public string AiScripts { get; set; } = "";
    public string Events { get; set; } = "";
    public string Sfx { get; set; } = "";

    /// <summary>File name format of effect bundles, <c>{0}</c> = entry of <see cref="GameDataSettings.FfxFiles"/>.</summary>
    public string SfxFileFormat { get; set; } = "";

    /// <summary>GameParam bundle path (without DCX suffix).</summary>
    public string GameParam { get; set; } = "";

    internal IEnumerable<string> Validate() =>
        ValidationHelpers.RequireText(MapStudio, "GameData.Folders.MapStudio")
            .Concat(ValidationHelpers.RequireText(AiScripts, "GameData.Folders.AiScripts"))
            .Concat(ValidationHelpers.RequireText(Events, "GameData.Folders.Events"))
            .Concat(ValidationHelpers.RequireText(Sfx, "GameData.Folders.Sfx"))
            .Concat(ValidationHelpers.RequireText(SfxFileFormat, "GameData.Folders.SfxFileFormat"))
            .Concat(ValidationHelpers.RequireText(GameParam, "GameData.Folders.GameParam"));
}

/// <summary>One game map (an .msb file plus its scripts and events).</summary>
public sealed class MapDefinition
{
    /// <summary>File stem, e.g. <c>m10_00_00_00</c>.</summary>
    public string Id { get; set; } = "";

    /// <summary>Area name shown in logs.</summary>
    public string Name { get; set; } = "";

    /// <summary>
    /// False for maps that are not randomized themselves but derived from another map
    /// (m12_00_00_01 is copied from m12_00_00_00).
    /// </summary>
    public bool Randomized { get; set; } = true;

    /// <summary>For derived maps: the randomized map whose creatures are copied after randomizing.</summary>
    public string? CopyOf { get; set; }

    /// <summary>True when <c>original_enemies_ref</c> has separate <c>.ptde</c>/<c>.remaster</c> files.</summary>
    public bool HasEditionSpecificReference { get; set; }

    public int? StartEventEntityId { get; set; }

    public int? DisableRespawnEventId { get; set; }

    /// <summary>Index of the first added character model in the .msb model list, per edition.</summary>
    public Dictionary<GameEdition, int> StartIndices { get; set; } = [];

    internal IEnumerable<string> Validate()
    {
        foreach (var error in ValidationHelpers.RequireText(Name, $"Map '{Id}' name"))
        {
            yield return error;
        }

        foreach (var edition in Enum.GetValues<GameEdition>())
        {
            if (!StartIndices.TryGetValue(edition, out var index))
            {
                yield return $"Map '{Id}' has no start index for '{edition}'.";
            }
            else if (index <= 0)
            {
                yield return $"Map '{Id}' start index for '{edition}' must be positive.";
            }
        }

        if (Randomized && (StartEventEntityId is null || DisableRespawnEventId is null))
        {
            yield return $"Randomized map '{Id}' needs StartEventEntityId and DisableRespawnEventId.";
        }
    }
}

/// <summary>Values that differ between PTDE and Remastered.</summary>
public sealed class GameEditionDefinition
{
    public string ExeName { get; set; } = "";

    /// <summary>Files are DCX compressed and carry <see cref="GameDataSettings.CompressedSuffix"/>.</summary>
    public bool UsesDcx { get; set; }

    /// <summary>Known SHA-256 hashes (lower-case hex) of the exe and the state they stand for.</summary>
    public Dictionary<string, ExeState> ExeHashes { get; set; } = [];

    /// <summary>Memory patch applied to the exe (PTDE only).</summary>
    public ExePatchSettings? ExePatch { get; set; }

    internal IEnumerable<string> Validate(GameEdition edition)
    {
        foreach (var error in ValidationHelpers.RequireText(ExeName, $"GameData.Editions.{edition}.ExeName"))
        {
            yield return error;
        }

        foreach (var hash in ExeHashes.Keys.Where(h => h.Length != 64 || !h.All(Uri.IsHexDigit)))
        {
            yield return $"GameData.Editions.{edition}: '{hash}' is not a SHA-256 hex string.";
        }

        if (ExePatch is not null)
        {
            foreach (var error in ExePatch.Validate(edition))
            {
                yield return error;
            }
        }
    }
}

/// <summary>
/// Byte patch that raises the PTDE memory limit (fix by Metal Crow, legacy <c>check_exe.py:54-91</c>).
/// </summary>
public sealed class ExePatchSettings
{
    /// <summary>Bytes to find, as space separated hex (e.g. <c>68 00 00 A2 00</c>).</summary>
    public string Search { get; set; } = "";

    /// <summary>Replacement bytes, same length as <see cref="Search"/>.</summary>
    public string Replace { get; set; } = "";

    public string BackupName { get; set; } = "";

    public string DebugBackupName { get; set; } = "";

    /// <summary>Parses a space separated hex string into bytes.</summary>
    public static byte[] ParseHex(string hex) =>
        hex.Split(' ', StringSplitOptions.RemoveEmptyEntries).Select(b => Convert.ToByte(b, 16)).ToArray();

    internal IEnumerable<string> Validate(GameEdition edition)
    {
        var prefix = $"GameData.Editions.{edition}.ExePatch";
        byte[] search;
        byte[] replace;
        try
        {
            search = ParseHex(Search);
            replace = ParseHex(Replace);
        }
        catch (FormatException)
        {
            return [$"{prefix}: Search/Replace must be space separated hex bytes."];
        }

        var errors = new List<string>();
        if (search.Length == 0 || search.Length != replace.Length)
        {
            errors.Add($"{prefix}: Search and Replace must be non-empty and of equal length.");
        }

        errors.AddRange(ValidationHelpers.RequireText(BackupName, $"{prefix}.BackupName"));
        errors.AddRange(ValidationHelpers.RequireText(DebugBackupName, $"{prefix}.DebugBackupName"));
        return errors;
    }
}
