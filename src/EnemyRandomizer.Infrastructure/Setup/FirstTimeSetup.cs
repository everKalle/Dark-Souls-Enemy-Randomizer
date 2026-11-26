using System.Globalization;
using EnemyRandomizer.Core;
using EnemyRandomizer.Core.Abstractions;
using EnemyRandomizer.Core.Configuration;
using EnemyRandomizer.Core.Installation;
using EnemyRandomizer.Formats.Bnd;
using EnemyRandomizer.Formats.Ffx;
using EnemyRandomizer.Formats.Lua;
using EnemyRandomizer.Formats.Msb;
using EnemyRandomizer.Infrastructure.Game;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EnemyRandomizer.Infrastructure.Setup;

/// <summary>
/// Prepares everything the randomizer works from (legacy <c>firstTimeSetup</c>, <c>randomizer_rng.py:501-637</c>):
/// <list type="number">
/// <item>clean .msb copies with all character models appended (so any enemy can be placed in any map),</item>
/// <item>original .luabnd copies and the extracted AI scripts,</item>
/// <item>folders for logs and layout references, a GameParam copy,</item>
/// <item>all map effects merged into CommonEffects (PTDE: map bundles reduced to their first entry).</item>
/// </list>
/// Every step skips work that is already done, so running it again is cheap. Copies are made from the
/// <c>.bak</c> original when one exists (v0.4.x used the current, possibly randomized, file).
/// </summary>
public sealed partial class FirstTimeSetup(
    IGamePaths paths,
    GameFileLayout layout,
    IOptions<GameDataSettings> gameData,
    IOptions<MsbModelSettings> models,
    ILogger<FirstTimeSetup> logger) : IFirstTimeSetup
{
    private const string CommonEffects = "CommonEffects";
    private const int CharacterModelType = 2;
    private static readonly string[] ScriptsThatMustExist = ["120000_battle.lua", "540000_battle.lua"];

    public SetupResult Run(GameEdition edition, IProgress<OperationProgress> progress, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(progress);
        var randomized = layout.RandomizedMaps.ToList();
        var total = layout.Maps.Count + randomized.Count + 3;
        var step = 0;

        Directory.CreateDirectory(paths.MapStudioCopyDirectory);
        Directory.CreateDirectory(paths.MapAiCopyDirectory);
        foreach (var map in layout.Maps)
        {
            cancellationToken.ThrowIfCancellationRequested();
            progress.Report(new OperationProgress(++step, total, $"Preparing map copy: {map.Name}"));
            PrepareMapCopy(map, edition);
        }

        foreach (var map in randomized)
        {
            progress.Report(new OperationProgress(++step, total, $"Copying AI scripts: {map.Name}"));
            PrepareAiCopy(map, edition);
        }

        progress.Report(new OperationProgress(++step, total, "Extracting AI scripts"));
        ExtractAiScripts(randomized, edition);
        CleanupV032Backups(edition);

        progress.Report(new OperationProgress(++step, total, "Backing up GameParam"));
        Directory.CreateDirectory(paths.LogDirectory);
        Directory.CreateDirectory(paths.LayoutReferenceDirectory);
        BackupGameParam(edition);

        progress.Report(new OperationProgress(++step, total, "Preparing effect files (can take a while)"));
        var effectsFailed = !MergeEffects(edition);

        return Verify(edition) with { EffectsFailed = effectsFailed };
    }

    public SetupResult Verify(GameEdition edition)
    {
        var mapCopies = layout.Maps.Select(m => MapCopy(m.Id)).ToList();
        var aiCopies = layout.RandomizedMaps.Select(m => AiCopy(m.Id, edition)).ToList();
        return new SetupResult(
            mapCopies.Count,
            mapCopies.Count(f => !File.Exists(f)),
            mapCopies.Count(f => File.Exists(f) && !BackupFiles.IsValidCopy(f)),
            aiCopies.Count,
            aiCopies.Count(f => !File.Exists(f)),
            aiCopies.Count(f => File.Exists(f) && !BackupFiles.IsValidCopy(f)),
            EffectsFailed: false);
    }

    internal string MapCopy(string mapId) => Path.Combine(paths.MapStudioCopyDirectory, mapId + ".msb");

    internal string AiCopy(string mapId, GameEdition edition) =>
        Path.Combine(paths.MapAiCopyDirectory, Path.GetFileName(layout.AiScript(mapId, edition)));

    private int AddedModelCount => models.Value.AllModels.Count();

    private void PrepareMapCopy(MapDefinition map, GameEdition edition)
    {
        var copy = MapCopy(map.Id);
        var expectedModels = map.StartIndices[edition] + AddedModelCount;
        if (BackupFiles.IsValidCopy(copy) && MsbFile.Read(File.ReadAllBytes(copy)).Models.Rows.Count >= expectedModels)
        {
            return;
        }

        var msb = MsbFile.Read(File.ReadAllBytes(BackupFiles.Original(paths.InGame(layout.MapStudio(map.Id)))));
        AppendCharacterModels(msb);
        AtomicFile.WriteAllBytes(copy, msb.Write());
        LogMapCopy(logger, map.Id);
    }

    /// <summary>Appends every character model after the map's last one (legacy <c>randomizer_rng.py:546-554</c>).</summary>
    internal void AppendCharacterModels(MsbFile msb)
    {
        var lastIndex = msb.Models.Rows
            .Where(r => r.GetInt("Type") == CharacterModelType)
            .Select(r => r.GetInt("Index"))
            .DefaultIfEmpty(0)
            .Max();

        var index = lastIndex + 1;
        foreach (var model in models.Value.AllModels)
        {
            const int nameOffset = 32;
            var sibPath = string.Format(CultureInfo.InvariantCulture, models.Value.SibPathFormat, model);
            msb.Models.Add(nameOffset, CharacterModelType, index++, nameOffset + model.Length + 1, 1, 0, 0, 0, model, sibPath);
        }
    }

    private void PrepareAiCopy(MapDefinition map, GameEdition edition)
    {
        var copy = AiCopy(map.Id, edition);
        if (!BackupFiles.IsValidCopy(copy))
        {
            File.Copy(BackupFiles.Original(paths.InGame(layout.AiScript(map.Id, edition))), copy, overwrite: true);
        }
    }

    private void ExtractAiScripts(IEnumerable<MapDefinition> maps, GameEdition edition)
    {
        if (ScriptsThatMustExist.All(s => File.Exists(Path.Combine(paths.AiScriptDirectory, s))))
        {
            return;
        }

        Directory.CreateDirectory(paths.AiScriptDirectory);
        var written = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var map in maps)
        {
            var archive = LuaBnd.Read(File.ReadAllBytes(BackupFiles.Original(paths.InGame(layout.AiScript(map.Id, edition)))));
            foreach (var (fileName, data) in archive.AiScripts())
            {
                var target = Path.Combine(paths.AiScriptDirectory, fileName);
                if (written.Add(fileName) && !File.Exists(target))
                {
                    AtomicFile.WriteAllBytes(target, data);
                }
            }
        }
    }

    /// <summary>
    /// v0.3.2 wrote .msb backups that already contained the added models. When detected, the backups are rebuilt
    /// from the clean copies with the added models cut off (legacy <c>cleanupV032backup</c>).
    /// </summary>
    private void CleanupV032Backups(GameEdition edition)
    {
        var first = layout.RandomizedMaps.First();
        var firstBackup = BackupFiles.BackupOf(paths.InGame(layout.MapStudio(first.Id)));
        if (!File.Exists(firstBackup)
            || MsbFile.Read(File.ReadAllBytes(firstBackup)).Models.Rows.Count < first.StartIndices[edition] + AddedModelCount)
        {
            return;
        }

        LogV032Cleanup(logger);
        foreach (var map in layout.RandomizedMaps.Where(m => File.Exists(MapCopy(m.Id))))
        {
            var msb = MsbFile.Read(File.ReadAllBytes(MapCopy(map.Id)));
            msb.Models.Rows.RemoveRange(map.StartIndices[edition], msb.Models.Rows.Count - map.StartIndices[edition]);
            AtomicFile.WriteAllBytes(BackupFiles.BackupOf(paths.InGame(layout.MapStudio(map.Id))), msb.Write());
        }
    }

    private void BackupGameParam(GameEdition edition)
    {
        Directory.CreateDirectory(paths.ParamBackupDirectory);
        var source = paths.InGame(layout.GameParam(edition));
        var target = Path.Combine(paths.ParamBackupDirectory, Path.GetFileName(source));
        if (!BackupFiles.IsValidCopy(target))
        {
            // Copy through a temp file so an interrupted copy never leaves a truncated "original" behind.
            AtomicFile.WriteAllBytes(target, File.ReadAllBytes(source));
        }
    }

    /// <summary>Returns false when CommonEffects could not be written (legacy <c>ffxAddSuccessful</c>).</summary>
    private bool MergeEffects(GameEdition edition)
    {
        var mapBundles = gameData.Value.FfxFiles.Where(f => f != CommonEffects)
                                 .Select(f => paths.InGame(layout.Sfx(f, edition)))
                                 .ToList();
        var commonFile = paths.InGame(layout.Sfx(CommonEffects, edition));

        try
        {
            var common = GameArchive.Read(commonFile);
            var sources = mapBundles.Select(f => (IReadOnlyList<BndEntry>)GameArchive.Read(BackupFiles.Original(f)).Entries);
            var (merged, changed) = EffectMerger.Merge(sources, common.Entries, edition == GameEdition.Remastered);
            BackupFiles.EnsureBackup(commonFile);
            if (changed)
            {
                AtomicFile.WriteAllBytes(commonFile, common.ToBytes(merged));
                LogEffectsMerged(logger, merged.Count);
            }

            if (edition == GameEdition.Ptde)
            {
                foreach (var bundle in mapBundles)
                {
                    var archive = GameArchive.Read(bundle);
                    if (archive.Entries.Count > 1)
                    {
                        BackupFiles.WriteWithBackup(bundle, archive.ToBytes(EffectMerger.TruncateMapBundle(archive.Entries)));
                    }
                }
            }

            return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            LogEffectsFailed(logger, ex);
            return false;
        }
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Prepared map copy {MapId}")]
    private static partial void LogMapCopy(ILogger logger, string mapId);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Detected invalid .msb backups from v0.3.2, rebuilding them")]
    private static partial void LogV032Cleanup(ILogger logger);

    [LoggerMessage(Level = LogLevel.Information, Message = "CommonEffects written with {Count} entries")]
    private static partial void LogEffectsMerged(ILogger logger, int count);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Effect files could not be written")]
    private static partial void LogEffectsFailed(ILogger logger, Exception exception);
}
