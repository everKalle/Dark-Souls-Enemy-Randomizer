using EnemyRandomizer.Core;
using EnemyRandomizer.Core.Configuration;
using Microsoft.Extensions.Options;

namespace EnemyRandomizer.Infrastructure.Game;

/// <summary>
/// Bridge between "which game file" and "how it is named on this edition": game files are the same per map,
/// only Remastered appends the DCX suffix (<c>.msb</c> files are never compressed).
/// Paths are relative to the game folder.
/// </summary>
public sealed class GameFileLayout(IOptions<GameDataSettings> gameData)
{
    private GameDataSettings Data => gameData.Value;

    public IReadOnlyList<MapDefinition> Maps => Data.Maps;

    /// <summary>Maps with their own scripts and events (all except the derived Darkroot copy).</summary>
    public IEnumerable<MapDefinition> RandomizedMaps => Data.Maps.Where(m => m.Randomized);

    public IReadOnlyList<string> FfxFiles => Data.FfxFiles;

    public string MapStudio(string mapId) => $"{Data.Folders.MapStudio}/{mapId}.msb";

    public string AiScript(string mapId, GameEdition edition) => Compressed($"{Data.Folders.AiScripts}/{mapId}.luabnd", edition);

    public string Event(string mapId, GameEdition edition) => Compressed($"{Data.Folders.Events}/{mapId}.emevd", edition);

    public string Sfx(string name, GameEdition edition) =>
        Compressed($"{Data.Folders.Sfx}/{string.Format(System.Globalization.CultureInfo.InvariantCulture, Data.Folders.SfxFileFormat, name)}", edition);

    public string GameParam(GameEdition edition) => Compressed(Data.Folders.GameParam, edition);

    /// <summary>Reference file names in <c>original_enemies_ref</c> for a map (one, or one per edition).</summary>
    public static IEnumerable<string> OriginalReferenceFiles(MapDefinition map)
    {
        ArgumentNullException.ThrowIfNull(map);
        return map.HasEditionSpecificReference
            ? [$"{map.Id}.ptde.txt", $"{map.Id}.remaster.txt"]
            : [$"{map.Id}.txt"];
    }

    /// <summary>The reference file used for <paramref name="edition"/> (legacy <c>randomizer_rng.py:1394-1404</c>).</summary>
    public static string OriginalReferenceFile(MapDefinition map, GameEdition edition)
    {
        ArgumentNullException.ThrowIfNull(map);
        return !map.HasEditionSpecificReference ? $"{map.Id}.txt"
            : edition == GameEdition.Remastered ? $"{map.Id}.remaster.txt"
            : $"{map.Id}.ptde.txt";
    }

    /// <summary>Extension shown in messages, e.g. <c>.luabnd.dcx</c>.</summary>
    public static string Extension(string relativePath) =>
        relativePath[relativePath.LastIndexOf('/')..].Split('.', 2) is [_, var ext] ? "." + ext : "";

    private string Compressed(string path, GameEdition edition) =>
        Data.Editions[edition].UsesDcx ? path + Data.CompressedSuffix : path;
}
