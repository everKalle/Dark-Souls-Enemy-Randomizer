using System.Globalization;
using System.Reflection;
using EnemyRandomizer.Core;
using EnemyRandomizer.Core.Abstractions;
using EnemyRandomizer.Core.Configuration;
using EnemyRandomizer.Core.Data;
using EnemyRandomizer.Core.Randomization;
using EnemyRandomizer.Core.Settings;
using EnemyRandomizer.Formats.Dcx;
using EnemyRandomizer.Formats.Emevd;
using EnemyRandomizer.Formats.Msb;
using EnemyRandomizer.Formats.Param;
using EnemyRandomizer.Infrastructure.Game;
using Microsoft.Extensions.Options;

namespace EnemyRandomizer.Infrastructure.Randomization;

/// <summary>Result of a randomization run.</summary>
public sealed record RandomizationResult(string Seed, int Replaced, string LogFile, string LayoutFile);

/// <summary>
/// One complete randomization or placement copy (legacy <c>Randomizer.randomize</c>): prepares copies, resets the event files and
/// copies the randomizer's custom events, patches NpcParam, plans and writes every map, copies Darkroot Garden and
/// writes the log and the layout reference. Every changed game file keeps its original as <c>.bak</c>.
/// </summary>
public sealed class RandomizationRun(
    IGamePaths paths,
    GameFileLayout layout,
    IFirstTimeSetup setup,
    IGameDataRepository data,
    IOptions<GameDataSettings> gameData,
    IOptions<ReplacementRuleSettings> rules,
    IOptions<MapPatchSettings> patches,
    IOptions<DifficultyCurveSettings> curves,
    IOptions<RandomizerOptionSettings> optionSettings)
{
    private readonly BossSoulPatcher bossSouls = new(paths, layout, gameData, rules);
    private readonly DerivedMapCopier derivedMaps = new(paths, layout);

    private const string Ptde = "PTDE";
    private const string Remastered = "REMASTER";

    public RandomizationResult Run(RandomizerSettings settings, GameEdition edition, IProgress<OperationProgress> progress, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(progress);

        var seed = string.IsNullOrWhiteSpace(settings.Seed) ? SeededRandom.NewSeed() : settings.Seed;
        var options = new RunOptions(settings.Normalize(optionSettings.Value) with { Seed = seed });
        return Execute(settings.EnemyConfig, options, edition, source: null, progress, cancellationToken);
    }

    /// <summary>
    /// Replays a saved enemy layout instead of drawing random numbers (legacy <c>CopyFromReference</c>). Everything
    /// else is the same as a normal run, but no new layout reference is written.
    /// </summary>
    public RandomizationResult CopyPlacement(LayoutReference source, GameEdition edition, IProgress<OperationProgress> progress, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(progress);

        if (source.Remastered != (edition == GameEdition.Remastered))
        {
            throw new InvalidDataException(source.Remastered
                ? "Trying to load a reference file from Remastered while using the randomizer on Prepare to Die Edition."
                : "Trying to load a reference file from Prepare to Die Edition while using the randomizer on Remastered.");
        }

        var missing = layout.RandomizedMaps.FirstOrDefault(m => !source.Maps.ContainsKey(m.Id));
        if (missing is not null)
        {
            throw new InvalidDataException($"Map {missing.Id} ({missing.Name}) is missing from the reference file.");
        }

        // Settings the replay depends on come from the file; all other options are irrelevant without a draw.
        var defaults = RandomizerSettings.CreateDefault(optionSettings.Value);
        var values = defaults.Values.ToDictionary(v => v.Key, v => v.Value);
        values["BossSoulDrops"] = source.BossSouls;
        values["PinwheelChaos"] = source.PinwheelChaos;
        values["RespawningBosses"] = source.RoamingBossRespawn;
        var options = new RunOptions(new RandomizerSettings(values, "", RandomizerSettings.DefaultEnemyConfig));
        return Execute(RandomizerSettings.DefaultEnemyConfig, options, edition, source, progress, cancellationToken);
    }

    private RandomizationResult Execute(
        string enemyConfig, RunOptions options, GameEdition edition, LayoutReference? source,
        IProgress<OperationProgress> progress, CancellationToken cancellationToken)
    {
        progress.Report(new OperationProgress(1, 2 + (layout.RandomizedMaps.Count() * 4), "Checking and preparing files"));
        setup.Run(edition, new Progress<OperationProgress>(), cancellationToken);

        // Every game file is captured before its first change, so a failure or cancel leaves the game as it was.
        var transaction = new FileTransaction();
        try
        {
            return Apply(transaction, enemyConfig, options, edition, source, progress, cancellationToken);
        }
        catch
        {
            var failed = transaction.Rollback();
            if (failed.Count > 0)
            {
                throw new IOException(
                    $"The run failed and these files could not be put back, use 'Revert to normal': {string.Join(", ", failed.Select(Path.GetFileName))}");
            }

            throw;
        }
    }

    private RandomizationResult Apply(
        FileTransaction transaction, string enemyConfig, RunOptions options, GameEdition edition, LayoutReference? source,
        IProgress<OperationProgress> progress, CancellationToken cancellationToken)
    {
        var seed = options.Settings.Seed;
        var verb = source is null ? "Randomizing" : "Copying";
        var maps = layout.RandomizedMaps.ToList();
        var total = 2 + (maps.Count * 4);
        var step = 1;
        var time = DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss", CultureInfo.InvariantCulture);
        var log = new RunLog();
        var reference = new LayoutReferenceWriter();
        PrepareEventFiles(edition, transaction);
        var templates = DkScript.Compile(File.ReadAllText(paths.InData(patches.Value.EventTemplates.File)))
                                .Events.ToDictionary(e => e.Id);

        progress.Report(new OperationProgress(++step, total, "Loading files"));
        var pool = data.LoadPool(enemyConfig, edition, options.HostileNpcs);
        var planner = new ReplacementPlanner(
            pool,
            data.ReplacementTargets,
            options,
            new PlannerSettings(rules.Value, patches.Value, curves.Value, gameData.Value.MimicModelId),
            new SeededRandom(seed));

        WriteHeader(log, reference, options, enemyConfig, edition, source);
        bossSouls.Apply(edition, options, transaction);

        var replaced = 0;
        foreach (var map in maps)
        {
            cancellationToken.ThrowIfCancellationRequested();
            progress.Report(new OperationProgress(++step, total, $"{verb} {map.Name}"));
            log.Line($"{verb} {map.Id} - {map.Name}");
            reference.BeginMap(map.Id);

            var mapFile = paths.InGame(layout.MapStudio(map.Id));
            var msb = MsbFile.Read(File.ReadAllBytes(Path.Combine(paths.MapStudioCopyDirectory, map.Id + ".msb")));
            var aiFile = paths.InGame(layout.AiScript(map.Id, edition));
            var ai = new MapAiScripts(File.ReadAllBytes(Path.Combine(paths.MapAiCopyDirectory, Path.GetFileName(aiFile))), paths.AiScriptDirectory);
            var eventFile = paths.InGame(layout.Event(map.Id, edition));
            var events = new MapEventEditor(ReadEvents(eventFile), map.Id, (uint)Required(map.DisableRespawnEventId, map, nameof(map.DisableRespawnEventId)), templates, patches.Value);

            var creatures = data.LoadOriginalEnemies(map.Id, edition)
                .Select((c, row) => new MapCreature(c.PartName, c.Size, row < msb.Creatures.Rows.Count ? msb.Creatures.Rows[row].GetInt("AnimID") : -1))
                .ToList();
            var modelStart = map.StartIndices[edition];
            var mapLayout = source?.Maps[map.Id];
            var plan = mapLayout is null
                ? planner.PlanMap(map.Id, edition, creatures)
                : planner.PlanMapFromLayout(map.Id, edition, creatures, mapLayout, modelStart);

            // v0.4.x continues one id after the saved "levID" (the next free id) when copying; kept for identical files.
            var firstEntityId = mapLayout is null ? Required(map.StartEventEntityId, map, nameof(map.StartEventEntityId)) : mapLayout.NextEventEntityId + 1;
            var applier = new MapPlanApplier(msb, ai, events, data.AiReferences, modelStart, firstEntityId);
            applier.Apply(plan, reference, log);
            replaced += plan.Decisions.Count(d => d.Outcome == ReplacementOutcome.Replaced);

            progress.Report(new OperationProgress(++step, total, $"{verb} {map.Name} - saving scripts"));
            transaction.WriteWithBackup(aiFile, ai.Write());
            progress.Report(new OperationProgress(++step, total, $"{verb} {map.Name} - saving events"));
            var eventBytes = events.File.Write();
            transaction.WriteWithBackup(eventFile, edition == GameEdition.Remastered ? DcxFile.Write(DcxHeader.Emevd, eventBytes) : eventBytes);
            progress.Report(new OperationProgress(++step, total, $"{verb} {map.Name} - saving map"));
            transaction.WriteWithBackup(mapFile, msb.Write());

            log.Line("---------------------");
            reference.EndMap(applier.NextEventEntityId);
        }

        derivedMaps.Copy(edition, data.ReplacementTargets, transaction);

        var logFile = Path.Combine(paths.LogDirectory, $"rlog{time}.txt");
        var layoutFile = "";
        log.Save(logFile);
        if (source is null)
        {
            layoutFile = Path.Combine(paths.LayoutReferenceDirectory, $"enemy-layout-{time}.txt");
            reference.Save(layoutFile);
        }

        return new RandomizationResult(seed, replaced, logFile, layoutFile);
    }

    /// <summary>
    /// Restores the original event files (so changes are not applied twice), then copies the randomizer's own
    /// event files from <c>enemyRandomizerData/emevd/&lt;edition&gt;</c> (legacy <c>revertEmevds</c> + <c>applyEmevd</c>).
    /// </summary>
    private void PrepareEventFiles(GameEdition edition, FileTransaction transaction)
    {
        foreach (var map in layout.RandomizedMaps)
        {
            transaction.Restore(paths.InGame(layout.Event(map.Id, edition)));
        }

        var custom = Path.Combine(paths.EmevdDirectory, edition == GameEdition.Remastered ? Remastered : Ptde);
        if (!Directory.Exists(custom))
        {
            return;
        }

        foreach (var map in layout.RandomizedMaps)
        {
            var target = paths.InGame(layout.Event(map.Id, edition));
            var source = Path.Combine(custom, Path.GetFileName(target));
            if (File.Exists(source))
            {
                BackupFiles.EnsureBackup(target);
                transaction.Copy(source, target);
            }
        }
    }

    private static int Required(int? value, MapDefinition map, string field) =>
        value ?? throw new InvalidDataException($"Map {map.Id} ({map.Name}) has no {field} in the game data settings.");

    private static EmevdFile ReadEvents(string file)
    {
        var bytes = File.ReadAllBytes(file);
        return EmevdFile.Read(DcxFile.IsDcx(bytes) ? DcxFile.Read(bytes).Data : bytes);
    }

    private static void WriteHeader(RunLog log, LayoutReferenceWriter reference, RunOptions options, string enemyConfig, GameEdition edition, LayoutReference? source)
    {
        var version = Assembly.GetEntryAssembly()?.GetName().Version?.ToString(3) ?? "0.0.0";
        log.Line("----");
        log.Line($"Randomizer version {version} (.NET), mode {edition}");
        if (source is not null)
        {
            log.Line($"Copying the enemy placement of a reference file made by randomizer version {source.RandomizerVersion}.");
            log.Line($"Roaming boss soul drops: {source.BossSouls}%, pinwheel chaos: {source.PinwheelChaos}, roaming boss respawn disabled: {source.RoamingBossRespawn}");
            log.Line("----");
            return;
        }

        log.Line($"Enemy config: {enemyConfig}");
        foreach (var (key, value) in options.Settings.Values.OrderBy(v => v.Key, StringComparer.Ordinal))
        {
            log.Line($"{key}: {value}");
        }

        log.Line($"Seed: '{options.Settings.Seed}'");
        log.Line("----");
        reference.Header(version, edition == GameEdition.Remastered, options.BossSoulDrops, options.PinwheelChaos, options.DisableRoamingBossRespawn ? 1 : 0);
    }
}
