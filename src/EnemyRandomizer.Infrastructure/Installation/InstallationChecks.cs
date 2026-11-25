using EnemyRandomizer.Core;
using EnemyRandomizer.Core.Abstractions;
using EnemyRandomizer.Core.Configuration;
using EnemyRandomizer.Core.Installation;
using EnemyRandomizer.Infrastructure.Game;
using Microsoft.Extensions.Options;

namespace EnemyRandomizer.Infrastructure.Installation;

/// <summary>Composite: runs every registered check in order and joins their problems.</summary>
public sealed class CompositeInstallationCheck(IEnumerable<IInstallationCheck> checks) : IInstallationCheck
{
    private readonly IReadOnlyList<IInstallationCheck> _checks = checks.ToList();

    public IEnumerable<InstallationProblem> Check(GameEdition? edition, ExeState exeState) =>
        _checks.SelectMany(c => c.Check(edition, exeState));
}

/// <summary>The exe must be found and unpacked (legacy <c>randomizer.py:864-869</c>).</summary>
public sealed class ExeCheck : IInstallationCheck
{
    public IEnumerable<InstallationProblem> Check(GameEdition? edition, ExeState exeState)
    {
        var key = exeState switch
        {
            ExeState.NotUnpacked => MessageKeys.ExeNotUnpacked,
            ExeState.NotUnpackedDebug => MessageKeys.ExeDebugNotUnpacked,
            ExeState.NotFound => MessageKeys.ExeNotFound,
            _ => null,
        };

        return key is null ? [] : [new InstallationProblem(key)];
    }
}

/// <summary>
/// Game files the randomizer reads or writes: maps, AI scripts, events, effects, GameParam
/// (legacy <c>checkIfRightPlace</c>, <c>randomizer_rng.py:332-382</c>). Skipped when no game was found.
/// </summary>
public sealed class GameFilesCheck(IGamePaths paths, GameFileLayout layout) : IInstallationCheck
{
    public IEnumerable<InstallationProblem> Check(GameEdition? edition, ExeState exeState)
    {
        if (edition is not { } e)
        {
            return [];
        }

        var groups = new[]
        {
            layout.Maps.Select(m => layout.MapStudio(m.Id)).ToList(),
            layout.RandomizedMaps.Select(m => layout.AiScript(m.Id, e)).ToList(),
            layout.FfxFiles.Select(f => layout.Sfx(f, e)).ToList(),
            layout.RandomizedMaps.Select(m => layout.Event(m.Id, e)).ToList(),
        };

        var problems = groups.SelectMany(MissingIn).ToList();
        var gameParam = layout.GameParam(e);
        if (!File.Exists(paths.InGame(gameParam)))
        {
            problems.Add(new InstallationProblem(MessageKeys.DataFileMissing, gameParam));
        }

        if (problems.Count > 0)
        {
            var header = e == GameEdition.Remastered ? MessageKeys.GameFilesMissingRemastered : MessageKeys.GameFilesMissingPtde;
            problems.Insert(0, new InstallationProblem(header));
        }

        return problems;
    }

    private IEnumerable<InstallationProblem> MissingIn(IReadOnlyList<string> files)
    {
        var missing = files.Count(f => !File.Exists(paths.InGame(f)));
        if (missing == 0)
        {
            yield break;
        }

        var folder = files[0][..files[0].LastIndexOf('/')];
        yield return missing == files.Count
            ? new InstallationProblem(MessageKeys.NoFilesInFolder, folder)
            : new InstallationProblem(MessageKeys.FilesMissingInFolder, missing, files.Count, GameFileLayout.Extension(files[0]), folder);
    }
}

/// <summary>The randomizer data shipped in the zip (legacy <c>checkProperUnpack</c>, <c>randomizer_rng.py:384-411</c>).</summary>
public sealed class DataFilesCheck(IGamePaths paths, GameFileLayout layout, IOptions<AppSettings> app) : IInstallationCheck
{
    public IEnumerable<InstallationProblem> Check(GameEdition? edition, ExeState exeState)
    {
        var settings = app.Value;
        if (!Directory.Exists(paths.DataDirectory))
        {
            yield return new InstallationProblem(MessageKeys.DataFolderMissing, settings.DataFolder);
            yield break;
        }

        foreach (var file in settings.RequiredDataFiles.Where(f => !File.Exists(paths.InData(f))))
        {
            yield return new InstallationProblem(MessageKeys.DataFileMissing, $"{settings.DataFolder}/{file}");
        }

        var references = layout.Maps.SelectMany(GameFileLayout.OriginalReferenceFiles).ToList();
        var missing = references.Count(f => !File.Exists(Path.Combine(paths.OriginalEnemyReferenceDirectory, f)));
        var folder = $"{settings.DataFolder}/{settings.DataFolders.OriginalEnemyReferences}";
        if (missing == references.Count)
        {
            yield return new InstallationProblem(MessageKeys.NoOriginalReferences, folder);
        }
        else if (missing > 0)
        {
            yield return new InstallationProblem(MessageKeys.OriginalReferencesMissing, missing, references.Count, folder);
        }
    }
}

/// <summary>
/// Can the randomizer write game files? Legacy <c>checkIfAllowedToModify</c> overwrote a real .msb with test bytes;
/// this version never changes game files: it opens the first map for writing and creates/deletes a temp file
/// next to it (catches read-only files, missing folder rights and UAC-protected install folders).
/// </summary>
public sealed class WritePermissionCheck(IGamePaths paths, GameFileLayout layout) : IInstallationCheck
{
    public IEnumerable<InstallationProblem> Check(GameEdition? edition, ExeState exeState)
    {
        var firstMap = layout.RandomizedMaps.FirstOrDefault();
        var file = firstMap is null ? null : paths.InGame(layout.MapStudio(firstMap.Id));
        if (edition is null || file is null || !File.Exists(file))
        {
            return []; // nothing to test, the missing files are reported by GameFilesCheck
        }

        return CanWrite(file) ? [] : [new InstallationProblem(MessageKeys.NoWritePermission)];
    }

    internal static bool CanWrite(string file)
    {
        var probe = Path.Combine(Path.GetDirectoryName(file)!, $".write-test-{Guid.NewGuid():N}.tmp");
        try
        {
            using (new FileStream(file, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite))
            {
            }

            File.WriteAllBytes(probe, [0]);
            File.Delete(probe);
            return true;
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException or IOException)
        {
            return false;
        }
    }
}
