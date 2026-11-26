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
        var seed = options.Settings.Seed;
        var verb = source is null ? "Randomizing" : "Copying";
        var maps = layout.RandomizedMaps.ToList();
        var total = 2 + (maps.Count * 4);
        var step = 0;
        var time = DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss", CultureInfo.InvariantCulture);
        var log = new RunLog();
        var reference = new LayoutReferenceWriter();

        progress.Report(new OperationProgress(++step, total, "Checking and preparing files"));
        setup.Run(edition, new Progress<OperationProgress>(), cancellationToken);
        PrepareEventFiles(edition);
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
        ApplyBossSouls(edition, options);

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
            var events = new MapEventEditor(ReadEvents(eventFile), map.Id, (uint)map.DisableRespawnEventId!.Value, templates, patches.Value);

            var creatures = data.LoadOriginalEnemies(map.Id, edition)
                .Select((c, row) => new MapCreature(c.PartName, c.Size, row < msb.Creatures.Rows.Count ? msb.Creatures.Rows[row].GetInt("AnimID") : -1))
                .ToList();
            var modelStart = map.StartIndices[edition];
            var mapLayout = source?.Maps[map.Id];
            var plan = mapLayout is null
                ? planner.PlanMap(map.Id, edition, creatures)
                : planner.PlanMapFromLayout(map.Id, edition, creatures, mapLayout, modelStart);

            // v0.4.x continues one id after the saved "levID" (the next free id) when copying; kept for identical files.
            var firstEntityId = mapLayout is null ? map.StartEventEntityId!.Value : mapLayout.NextEventEntityId + 1;
            var applier = new MapPlanApplier(msb, ai, events, data.AiReferences, modelStart, firstEntityId);
            applier.Apply(plan, reference, log);
            replaced += plan.Decisions.Count(d => d.Outcome == ReplacementOutcome.Replaced);

            progress.Report(new OperationProgress(++step, total, $"{verb} {map.Name} - saving scripts"));
            BackupFiles.WriteWithBackup(aiFile, ai.Write());
            progress.Report(new OperationProgress(++step, total, $"{verb} {map.Name} - saving events"));
            var eventBytes = events.File.Write();
            BackupFiles.WriteWithBackup(eventFile, edition == GameEdition.Remastered ? DcxFile.Write(DcxHeader.Emevd, eventBytes) : eventBytes);
            progress.Report(new OperationProgress(++step, total, $"{verb} {map.Name} - saving map"));
            BackupFiles.WriteWithBackup(mapFile, msb.Write());

            log.Line("---------------------");
            reference.EndMap(applier.NextEventEntityId);
        }

        CopyDerivedMaps(edition, data.ReplacementTargets);

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
    private void PrepareEventFiles(GameEdition edition)
    {
        foreach (var map in layout.RandomizedMaps)
        {
            BackupFiles.Restore(paths.InGame(layout.Event(map.Id, edition)));
        }

        var custom = paths.InData(Path.Combine("emevd", edition == GameEdition.Remastered ? Remastered : Ptde));
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
                File.Copy(source, target, overwrite: true);
            }
        }
    }

    private static EmevdFile ReadEvents(string file)
    {
        var bytes = File.ReadAllBytes(file);
        return EmevdFile.Read(DcxFile.IsDcx(bytes) ? DcxFile.Read(bytes).Data : bytes);
    }

    /// <summary>"With souls" boss copies for bosses on normal enemy spots (legacy <c>applyBossSouls</c>).</summary>
    private void ApplyBossSouls(GameEdition edition, RunOptions options)
    {
        var file = paths.InGame(layout.GameParam(edition));
        var archive = GameArchive.Read(file);
        var index = gameData.Value.NpcParamIndex;
        var bosses = rules.Value.BossSouls.Select(b => new BossSoul(b.NpcParamId, b.Souls, b.Name)).ToList();

        var npcParam = NpcParam.Read(archive.Entries[index].Data);
        npcParam.AddSoulVariants(bosses);
        npcParam.ApplyBossSoulCount(bosses, options.BossSoulDrops);
        npcParam.SetRespawnFlags(bosses, disableRespawn: false, disableInitializeDead: false);
        npcParam.RemoveItemLots(rules.Value.NpcParamsWithoutItemLot);

        archive.Entries[index] = archive.Entries[index] with { Data = npcParam.Write() };
        File.WriteAllBytes(file, archive.ToBytes());
    }

    /// <summary>
    /// Maps derived from a randomized one (Darkroot Garden #2) get the same creatures (legacy <c>copyDarkrootGarden</c>):
    /// matching rows take model, AI, param, entity id, animation and position; extra rows are appended.
    /// </summary>
    private void CopyDerivedMaps(GameEdition edition, ReplacementTargets targets)
    {
        foreach (var map in layout.Maps.Where(m => m.CopyOf is not null))
        {
            var sourceMap = layout.Maps.Single(m => m.Id == map.CopyOf);
            var modelOffset = map.StartIndices[edition] - sourceMap.StartIndices[edition];
            var target = MsbFile.Read(File.ReadAllBytes(Path.Combine(paths.MapStudioCopyDirectory, map.Id + ".msb")));
            var source = MsbFile.Read(File.ReadAllBytes(paths.InGame(layout.MapStudio(sourceMap.Id))));
            var existing = target.Creatures.Rows.Count;

            for (var i = 0; i < source.Creatures.Rows.Count; i++)
            {
                var from = source.Creatures.Rows[i];
                if (i >= existing)
                {
                    // Fix: v0.4.x appended these rows (tail parts) without the model offset of the derived map.
                    var added = from.Clone();
                    added["Model"] = from.GetInt("Model") + modelOffset;
                    target.AddCreature(added);
                    continue;
                }

                var to = target.Creatures.Rows[i];
                if (targets.Find(to.Name[..Math.Min(5, to.Name.Length)]) is null)
                {
                    continue;
                }

                to["Model"] = from.GetInt("Model") + modelOffset;
                foreach (var field in new[] { "AI", "NPCParam", "EventEntityID", "AnimID", "X pos", "Y pos", "Z pos", "Rot X", "Rot Y", "Rot Z" })
                {
                    to[field] = from[field];
                }
            }

            for (var i = existing; i < source.Creatures.Rows.Count; i++)
            {
                target.Creatures.Rows[i]["PartIndex"] = source.Creatures.Rows[i]["PartIndex"];
            }

            BackupFiles.WriteWithBackup(paths.InGame(layout.MapStudio(map.Id)), target.Write());
        }
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
