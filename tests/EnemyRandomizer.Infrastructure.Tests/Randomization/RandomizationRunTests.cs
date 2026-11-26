using EnemyRandomizer.Core;
using EnemyRandomizer.Core.Abstractions;
using EnemyRandomizer.Core.Configuration;
using EnemyRandomizer.Core.Randomization;
using EnemyRandomizer.Core.Settings;
using EnemyRandomizer.Formats.Binary;
using EnemyRandomizer.Formats.Bnd;
using EnemyRandomizer.Formats.Dcx;
using EnemyRandomizer.Formats.Emevd;
using EnemyRandomizer.Formats.Lua;
using EnemyRandomizer.Formats.Msb;
using EnemyRandomizer.Formats.Param;
using EnemyRandomizer.Infrastructure.Data;
using EnemyRandomizer.Infrastructure.Game;
using EnemyRandomizer.Infrastructure.Randomization;
using EnemyRandomizer.Infrastructure.Setup;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace EnemyRandomizer.Infrastructure.Tests.Randomization;

/// <summary>Full randomization runs on a generated game folder shaped like the real one.</summary>
public sealed class RandomizationRunTests : IDisposable
{
    private readonly TempGameFolder _folder = new();
    private readonly GameDataSettings _gameData = ShippedSettings.Get<GameDataSettings>();
    private readonly GameFileLayout _layout;
    private readonly GameDataRepository _data;
    private readonly FirstTimeSetup _setup;
    private readonly RandomizationRun _run;

    public RandomizationRunTests()
    {
        _layout = new GameFileLayout(Options.Create(_gameData));
        _data = new GameDataRepository(_folder.Paths, _layout, ShippedSettings.Options<DifficultyCurveSettings>(), NullLogger<GameDataRepository>.Instance);
        _setup = new FirstTimeSetup(_folder.Paths, _layout, Options.Create(_gameData), ShippedSettings.Options<MsbModelSettings>(), NullLogger<FirstTimeSetup>.Instance);
        _run = new RandomizationRun(
            _folder.Paths, _layout, _setup, _data, Options.Create(_gameData),
            ShippedSettings.Options<ReplacementRuleSettings>(), ShippedSettings.Options<MapPatchSettings>(),
            ShippedSettings.Options<DifficultyCurveSettings>(), ShippedSettings.Options<RandomizerOptionSettings>());
    }

    public void Dispose() => _folder.Dispose();

    private static IProgress<OperationProgress> NoProgress => new Progress<OperationProgress>();

    private string Game(string relative) => _folder.Paths.InGame(relative);

    private static byte[] Unpack(string file)
    {
        var bytes = File.ReadAllBytes(file);
        return DcxFile.IsDcx(bytes) ? DcxFile.Read(bytes).Data : bytes;
    }

    private static RandomizerSettings Settings(string seed = "test seed", params (string Key, int Value)[] overrides)
    {
        var defaults = RandomizerSettings.CreateDefault(ShippedSettings.Get<RandomizerOptionSettings>());
        var values = defaults.Values.ToDictionary(v => v.Key, v => v.Value);
        foreach (var (key, value) in overrides)
        {
            values[key] = value;
        }

        return new RandomizerSettings(values, seed, RandomizerSettings.DefaultEnemyConfig);
    }

    /// <summary>Game files with one creature row per line of the shipped enemy lists, valid events and NpcParam.</summary>
    private void CreateGame(GameEdition edition)
    {
        byte[] Pack(byte[] content) => edition == GameEdition.Remastered ? DcxFile.Write(DcxHeader.Emevd, content) : content;

        _folder.CreateGameFiles(edition, _layout);
        _folder.CopyShippedData();

        foreach (var map in _layout.Maps)
        {
            var msb = new MsbFile();
            msb.Models.Add(32, 0, 0, 38, 1, 0, 0, 0, "m0000", "map.sib");
            msb.Models.Add(32, 2, 4, 38, 1, 0, 0, 0, "c2500", "chr.sib");
            foreach (var enemy in _data.LoadOriginalEnemies(map.Id, edition))
            {
                msb.Creatures.Rows.Add(Creature(enemy.PartName));
            }

            _folder.Touch(_layout.MapStudio(map.Id), msb.Write());
        }

        foreach (var map in _layout.RandomizedMaps)
        {
            var events = new EmevdFile();
            events.Events.Add(new EmevdEvent { Id = 0 });
            _folder.Touch(_layout.Event(map.Id, edition), Pack(events.Write()));
        }

        var schema = NpcParam.StandardSchema;
        var npcParam = new ParamFile { DataSize = schema.RowSize };
        foreach (var id in ShippedSettings.Get<ReplacementRuleSettings>().BossSouls.Select(b => b.NpcParamId).Prepend(100))
        {
            npcParam.Rows.Add(new ParamRow(id, "row" + id, new byte[schema.RowSize]));
        }

        var entries = Enumerable.Range(0, 13).Select(i => new BndEntry(i, $"param{i}.param", [(byte)i])).ToList();
        entries[_gameData.NpcParamIndex] = new BndEntry(12, "NpcParam.param", npcParam.Write());
        _folder.Touch(_layout.GameParam(edition), Pack(Bnd3.Write(entries)));

        Directory.CreateDirectory(_folder.Paths.AiScriptDirectory);
        foreach (var script in _data.AiReferences.Values.SelectMany(a => new[] { a.BattleScript, a.LogicScript }).Where(s => s.Length > 0).Distinct())
        {
            File.WriteAllBytes(Path.Combine(_folder.Paths.AiScriptDirectory, script), [1, 2, 3]);
        }
    }

    private static MsbRow Creature(string name)
    {
        var layout = MsbLayout.Get("creatures2");
        var codes = StructFormat.Codes(layout.BeforeName) + "ss" + StructFormat.Codes(layout.AfterName);
        var values = codes.Select(c => c switch { 'f' => (object)0f, 's' => "", _ => 0 }).ToArray();
        values[0] = 0x64;
        values[1] = 2;
        values[4] = 0x64 + name.Length + 1;
        values[layout.NameIndex] = name;
        values[layout.NameIndex + 1] = "chr.sib";
        var row = new MsbRow(layout, values);
        row["EventEntityID"] = -1;
        row["AnimID"] = -1;
        return row;
    }

    private RandomizationResult Randomize(GameEdition edition, RandomizerSettings? settings = null) =>
        _run.Run(settings ?? Settings(), edition, NoProgress, TestContext.Current.CancellationToken);

    [Theory]
    [InlineData(GameEdition.Ptde)]
    [InlineData(GameEdition.Remastered)]
    public void Randomize_rewrites_every_map_with_backups(GameEdition edition)
    {
        CreateGame(edition);

        var result = Randomize(edition);

        Assert.True(result.Replaced > 500, $"only {result.Replaced} replaced");
        foreach (var map in _layout.Maps)
        {
            var file = Game(_layout.MapStudio(map.Id));
            Assert.True(File.Exists(BackupFiles.BackupOf(file)), map.Id);

            var msb = MsbFile.Read(File.ReadAllBytes(file));
            Assert.Equal(2 + 131, msb.Models.Rows.Count);  // written from the copy with all added models
        }

        Assert.True(File.Exists(result.LogFile));
        Assert.Equal(17, File.ReadLines(result.LayoutFile).Count(l => l.StartsWith("section m", StringComparison.Ordinal)));
    }

    [Fact]
    public void Replaced_creatures_point_to_added_models_and_their_ai_is_known_to_the_map()
    {
        CreateGame(GameEdition.Remastered);
        Randomize(GameEdition.Remastered);

        var map = _gameData.Maps.Single(m => m.Id == "m10_01_00_00");
        var start = map.StartIndices[GameEdition.Remastered];
        var msb = MsbFile.Read(File.ReadAllBytes(Game(_layout.MapStudio(map.Id))));
        var info = LuaInfo.Read(LuaBnd.Read(File.ReadAllBytes(Game(_layout.AiScript(map.Id, GameEdition.Remastered)))).InfoData);
        var knownAis = info.Entries.Select(e => (int)e.NpcAi).ToHashSet();

        var replaced = msb.Creatures.Rows.Where(r => r.GetInt("Model") >= start).ToList();
        Assert.NotEmpty(replaced);
        Assert.All(replaced, r =>
        {
            Assert.InRange(r.GetInt("Model"), start, start + 130);
            if (r.GetInt("AI") != 1)    // tails have AI 1
            {
                Assert.Contains(r.GetInt("AI"), knownAis);
            }
        });
    }

    [Fact]
    public void Events_get_the_template_events_and_npc_param_the_soul_variants()
    {
        CreateGame(GameEdition.Ptde);
        Randomize(GameEdition.Ptde, Settings(overrides: ("RespawningBosses", 1)));

        var map = _gameData.Maps.Single(m => m.Id == "m10_00_00_00");
        var events = EmevdFile.Read(File.ReadAllBytes(Game(_layout.Event(map.Id, GameEdition.Ptde))));
        var id = (uint)map.DisableRespawnEventId!.Value;
        Assert.Contains(events.Events, e => e.Id == id);
        Assert.Contains(events.Events, e => e.Id == id + 200);
        Assert.Contains(events.Events, e => e.Id == id - 200);

        var param = NpcParam.Read(Bnd3.Read(Unpack(Game(_layout.GameParam(GameEdition.Ptde))))[12].Data);
        var straySouls = param.File.Rows.FindIndex(r => r.Id == 223050);
        Assert.True(straySouls >= 0);
        Assert.Equal(10000u, param.Records[straySouls].Values["getSoul"]);   // 50 % of 20000
    }

    [Fact]
    public void Custom_event_files_are_copied_before_editing()
    {
        CreateGame(GameEdition.Ptde);
        Randomize(GameEdition.Ptde);

        // m13_00 ships a custom event file: its events are kept, the templates are added
        var events = EmevdFile.Read(File.ReadAllBytes(Game(_layout.Event("m13_00_00_00", GameEdition.Ptde))));
        Assert.True(events.Events.Count > 10);
    }

    [Fact]
    public void Same_seed_gives_the_same_layout_and_randomizing_twice_starts_from_the_originals()
    {
        CreateGame(GameEdition.Ptde);
        var first = Randomize(GameEdition.Ptde);
        var firstMaps = _layout.Maps.Select(m => File.ReadAllBytes(Game(_layout.MapStudio(m.Id)))).ToList();
        var firstEvents = File.ReadAllBytes(Game(_layout.Event("m10_00_00_00", GameEdition.Ptde)));

        var second = Randomize(GameEdition.Ptde);

        Assert.Equal(first.Replaced, second.Replaced);
        Assert.Equal(firstMaps, _layout.Maps.Select(m => File.ReadAllBytes(Game(_layout.MapStudio(m.Id)))));
        Assert.Equal(firstEvents, File.ReadAllBytes(Game(_layout.Event("m10_00_00_00", GameEdition.Ptde))));
    }

    private static readonly string[] CopiedFields =
        ["Model", "AI", "NPCParam", "EventEntityID", "AnimID", "X pos", "Y pos", "Z pos", "Rot X", "Rot Y", "Rot Z"];

    private List<List<object?[]>> ReadCreatures() => _layout.Maps
        .Select(m => MsbFile.Read(File.ReadAllBytes(Game(_layout.MapStudio(m.Id)))).Creatures.Rows
            .Select(r => CopiedFields.Select(f => (object?)r[f]).ToArray()).ToList())
        .ToList();

    [Theory]
    [InlineData(GameEdition.Ptde)]
    [InlineData(GameEdition.Remastered)]
    public void Copy_placement_restores_the_saved_layout_without_randomness(GameEdition edition)
    {
        CreateGame(edition);
        var first = Randomize(edition, Settings("copy me", ("RespawningBosses", 1)));
        var randomized = ReadCreatures();
        var source = LayoutReference.Parse(File.ReadLines(first.LayoutFile)); // read now: a run in the same second reuses the file name
        _run.Run(Settings("another seed", ("RespawningBosses", 0)), edition, NoProgress, TestContext.Current.CancellationToken);
        Assert.NotEqual(randomized.SelectMany(m => m).Select(r => r[0]), ReadCreatures().SelectMany(m => m).Select(r => r[0]));

        var copy = _run.CopyPlacement(source, edition, NoProgress, TestContext.Current.CancellationToken);

        Assert.Equal(first.Replaced, copy.Replaced);
        Assert.Equal("", copy.LayoutFile);
        Assert.True(File.Exists(copy.LogFile));

        // Tail parts are appended after the original rows and may get other entity ids; compare the original rows.
        var copied = ReadCreatures();
        for (var map = 0; map < randomized.Count; map++)
        {
            var original = _data.LoadOriginalEnemies(_layout.Maps[map].Id, edition).Count;
            Assert.True(randomized[map].Take(original).Zip(copied[map]).All(p => p.First.SequenceEqual(p.Second)), $"{_layout.Maps[map].Id} differs");
        }
    }

    [Fact]
    public void Copy_placement_rejects_a_reference_of_the_other_edition()
    {
        CreateGame(GameEdition.Ptde);
        var layoutFile = Randomize(GameEdition.Ptde).LayoutFile;
        var source = LayoutReference.Parse(File.ReadLines(layoutFile));

        var error = Assert.Throws<InvalidDataException>(() =>
            _run.CopyPlacement(source, GameEdition.Remastered, NoProgress, TestContext.Current.CancellationToken));
        Assert.Contains("Prepare to Die Edition", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Empty_seed_gets_a_generated_one()
    {
        CreateGame(GameEdition.Ptde);
        var result = Randomize(GameEdition.Ptde, Settings(seed: ""));
        Assert.False(string.IsNullOrEmpty(result.Seed));
    }

    [Fact]
    public void Darkroot_copy_follows_the_randomized_darkroot()
    {
        CreateGame(GameEdition.Remastered);
        Randomize(GameEdition.Remastered);

        var source = MsbFile.Read(File.ReadAllBytes(Game(_layout.MapStudio("m12_00_00_00")))).Creatures.Rows;
        var copy = MsbFile.Read(File.ReadAllBytes(Game(_layout.MapStudio("m12_00_00_01")))).Creatures.Rows;
        var replaced = source.Select((r, i) => (r, i))
                             .Where(x => x.i < copy.Count && x.r.Name == copy[x.i].Name && x.r.GetInt("Model") >= 280)
                             .ToList();

        Assert.NotEmpty(replaced);
        Assert.All(replaced, x => Assert.Equal(x.r.GetInt("Model") + 2, copy[x.i].GetInt("Model")));
    }

    [Fact]
    public void Revert_after_randomize_restores_the_original_maps()
    {
        CreateGame(GameEdition.Remastered);
        var original = File.ReadAllBytes(Game(_layout.MapStudio("m15_01_00_00")));
        Randomize(GameEdition.Remastered);
        Assert.NotEqual(original, File.ReadAllBytes(Game(_layout.MapStudio("m15_01_00_00"))));

        new GameRestorer(_folder.Paths, _layout, new NoExePatcher(), Options.Create(_gameData), NullLogger<GameRestorer>.Instance)
            .Revert(GameEdition.Remastered, restoreEffects: false, NoProgress, TestContext.Current.CancellationToken);

        Assert.Equal(original, File.ReadAllBytes(Game(_layout.MapStudio("m15_01_00_00"))));
    }

    private sealed class NoExePatcher : IExePatcher
    {
        public bool PatchIfNeeded() => true;

        public bool Restore() => false;
    }
}
