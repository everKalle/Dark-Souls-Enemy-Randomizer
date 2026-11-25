using EnemyRandomizer.Core.Configuration;
using EnemyRandomizer.Core.Data;
using EnemyRandomizer.Core.Randomization;
using EnemyRandomizer.Core.Settings;

namespace EnemyRandomizer.Core.Tests.Randomization;

/// <summary>Returns scripted values (clamped to the asked range); falls back to the minimum.</summary>
internal sealed class ScriptedRandom(params int[] values) : IRandomSource
{
    private readonly Queue<int> _values = new(values);

    public double Percent { get; set; } = 99;

    public int Between(int min, int max) => _values.Count > 0 ? Math.Clamp(_values.Dequeue(), min, max) : min;

    public double NextPercent() => Percent;
}

public class PlannerTests
{
    private static EnemyDefinition E(int index, string id, EnemyType type, int size = 0, int difficulty = 0, int[]? ais = null, int[]? idle = null) =>
        new(index, id, id, type, "0", size, difficulty, [], ais ?? [index * 100], ais ?? [index * 100], idle ?? [-1], "");

    private static readonly EnemyDefinition[] Catalog =
    [
        E(0, "c2500", EnemyType.Normal),                          // Hollow
        E(1, "c2510", EnemyType.Normal, idle: [7000, 7001]),      // Lautrec model (nerfed)
        E(2, "c5290", EnemyType.Boss, size: 5, difficulty: 6),    // Seath
        E(3, "c5320", EnemyType.Boss, size: 5, difficulty: 6),    // Gwyndolin
        E(4, "c2640", EnemyType.Npc),                             // Andre
        E(5, "c5351", EnemyType.Boss, size: 5),                   // gargoyle, no soul variant
    ];

    private static readonly ReplacementTargets Targets = new(
    [
        new("c2500", "Hollow", EnemyType.Normal, 0),
        new("c2640", "Andre", EnemyType.Npc, 0),
        new("c5320", "Gwyndolin", EnemyType.Boss, 6),
        new("c2780", "Mimic", EnemyType.Normal, 0),
    ]);

    private static RandomizerSettings Settings(params (string Key, int Value)[] overrides)
    {
        var values = new Dictionary<string, int>
        {
            ["BossMode"] = 1, ["NormalMode"] = 2, ["NpcMode"] = 0, ["MimicMode"] = 1, ["FitMode"] = 1,
            ["DifficultyMode"] = 2, ["ReplaceChance"] = 100, ["BossChanceNormals"] = 10, ["BossChanceBosses"] = 90,
            ["GargoyleMode"] = 1, ["DiffStrictness"] = 1, ["TPoseCity"] = 0, ["BossSoulDrops"] = 50, ["PinwheelChaos"] = 1,
            ["TypeReplacement"] = 1, ["GwynNerf"] = 2, ["PreventSame"] = 1, ["UniqueBosses"] = 0, ["RespawningBosses"] = 0,
            ["HostileNpcs"] = 0, ["MosquitoReplacement"] = 1,
        };
        foreach (var (key, value) in overrides)
        {
            values[key] = value;
        }

        return new RandomizerSettings(values, "seed", "Default");
    }

    private static ReplacementRuleSettings Rules() => new()
    {
        UniqueEnemyLimit = new UniqueEnemyLimitSettings { Default = 30 },
        UniqueBossResetCount = 22,
        NoSoulVariant = ["c5351"],
        BannedPairings = [new BannedPairing { Reason = "flying", Originals = ["c5320"], Replacements = ["c5290"] }],
        RandomRejections = [new RandomRejection { Name = "Lautrec", ModelId = "c2510", ChancePercent = 65 }],
        CreatureRules =
        [
            new CreatureRule { Name = "skip", Parts = ["c2500_0099"], Skip = true },
            new CreatureRule { Name = "move", Maps = ["m10"], Parts = ["c2500_0001"], Position = [1, 2, 3], Rotation = [0, 90, 0] },
            new CreatureRule { Name = "andre", Parts = ["c2640"], When = new OptionCondition { Option = "NpcMode", Values = [1, 3] }, ForcedEnemyId = "c5320", ForcedChancePercent = 40 },
        ],
    };

    private static ReplacementPlanner Planner(IRandomSource random, RandomizerSettings? settings = null, ReplacementRuleSettings? rules = null, MapPatchSettings? patches = null, bool hostileNpcs = false) =>
        new(
            EnemyPool.Build(Catalog, GameEdition.Ptde, hostileNpcs, 7),
            Targets,
            new RunOptions(settings ?? Settings()),
            new PlannerSettings(rules ?? Rules(), patches ?? new MapPatchSettings(), Curves(), "c2780"),
            random);

    private static DifficultyCurveSettings Curves() => new()
    {
        MinClass = 0,
        MaxClass = 7,
        Offsets = [0],
        Curves = [new() { Chances = [100] }, new() { Chances = [100] }, new() { Chances = [100] }],
    };

    private static CreatureDecision Single(ReplacementPlanner planner, string part, string map = "m10", int size = 5, int idle = -1) =>
        planner.PlanMap(map, GameEdition.Ptde, [new MapCreature(part, size, idle)]).Decisions.Single();

    [Fact]
    public void Unknown_parts_are_not_targets()
    {
        Assert.Equal(ReplacementOutcome.NotATarget, Single(Planner(new ScriptedRandom()), "o1234_0000").Outcome);
    }

    [Fact]
    public void Skip_rules_keep_the_original()
    {
        Assert.Equal(ReplacementOutcome.SpecialCase, Single(Planner(new ScriptedRandom()), "c2500_0099").Outcome);
    }

    [Fact]
    public void Failed_chance_roll_keeps_the_original()
    {
        var planner = Planner(new ScriptedRandom(80), Settings(("ReplaceChance", 50)));
        Assert.Equal(ReplacementOutcome.RandomChance, Single(planner, "c2500_0000").Outcome);
    }

    [Fact]
    public void Normal_is_replaced_by_a_normal_with_its_ai_and_param()
    {
        // roll 1 (replace), pick index 0 of [c2500, c2510]
        var decision = Single(Planner(new ScriptedRandom(1, 0)), "c2500_0000");

        Assert.Equal(ReplacementOutcome.Replaced, decision.Outcome);
        Assert.Equal("c2500", decision.Enemy!.Id);
        Assert.Equal((0, 0, true), (decision.Ai, decision.Param, decision.ChangeAi));
    }

    [Fact]
    public void Random_rejection_and_prevent_same_try_the_next_candidate()
    {
        var random = new ScriptedRandom(1, 1, 0) { Percent = 10 };        // Lautrec rejected (10 < 65), then index 0
        var planner = Planner(random, Settings(("PreventSame", 0)));      // ...but c2500 is the same enemy: rejected too

        Assert.Equal(ReplacementOutcome.NoValidEnemy, Single(planner, "c2500_0000").Outcome);
    }

    [Fact]
    public void Boss_on_a_normal_spot_uses_the_soul_variant_param()
    {
        var planner = Planner(new ScriptedRandom(1, 0), Settings(("NormalMode", 1)));
        var decision = Single(planner, "c2500_0000");

        Assert.Equal("c5290", decision.Enemy!.Id);
        Assert.Equal(200 + 50, decision.Param);
    }

    [Fact]
    public void No_soul_variant_for_listed_bosses()
    {
        var planner = Planner(new ScriptedRandom(1, 2), Settings(("NormalMode", 1)));
        var decision = Single(planner, "c2500_0000");

        Assert.Equal("c5351", decision.Enemy!.Id);
        Assert.Equal(500, decision.Param);
    }

    [Fact]
    public void Banned_pairing_is_never_chosen()
    {
        // Gwyndolin spot: candidates [c5290, c5320, c5351]; index 0 (Seath) is banned, then index 0 again = c5320
        var decision = Single(Planner(new ScriptedRandom(1, 0, 0)), "c5320_0000");
        Assert.Equal("c5320", decision.Enemy!.Id);
    }

    [Fact]
    public void Npcs_keep_their_ai()
    {
        var planner = Planner(new ScriptedRandom(1, 0, 100), Settings(("NpcMode", 2)));
        var decision = Single(planner, "c2640_0000");

        Assert.Equal(ReplacementOutcome.Replaced, decision.Outcome);
        Assert.False(decision.ChangeAi);
    }

    [Fact]
    public void Andre_may_be_forced_to_gwyndolin()
    {
        var planner = Planner(new ScriptedRandom(1, 1, 0, 40), Settings(("NpcMode", 3)));  // roll 40 <= 40 %
        Assert.Equal("c5320", Single(planner, "c2640_0000").Enemy!.Id);
    }

    [Fact]
    public void Position_rule_applies_to_the_replacement()
    {
        var decision = Single(Planner(new ScriptedRandom(1, 0)), "c2500_0001");

        Assert.Equal([1f, 2f, 3f], decision.Position!);
        Assert.Equal([0f, 90f, 0f], decision.Rotation!);
    }

    [Fact]
    public void Type_replacement_reuses_the_first_choice_in_the_map()
    {
        var planner = Planner(new ScriptedRandom(1, 1, 1, 0), Settings(("TypeReplacement", 0)));

        var plan = planner.PlanMap("m10", GameEdition.Ptde, [new("c2500_0000", 5, -1), new("c2500_0001", 5, -1)]);

        Assert.Equal(["c2510", "c2510"], plan.Decisions.Select(d => d.Enemy!.Id));
    }

    [Fact]
    public void Unique_limit_reuses_already_placed_enemies()
    {
        var rules = Rules();
        rules.UniqueEnemyLimit.Default = 1;
        var planner = Planner(new ScriptedRandom(1, 1, 1, 0), rules: rules);

        var plan = planner.PlanMap("m10", GameEdition.Ptde, [new("c2500_0000", 5, -1), new("c2500_0002", 5, -1)]);

        Assert.Equal(["c2510", "c2510"], plan.Decisions.Select(d => d.Enemy!.Id));
    }

    [Fact]
    public void Fix_t_pose_picks_an_idle_animation_for_creatures_that_had_one()
    {
        var planner = Planner(new ScriptedRandom(1, 1, 1), Settings(("TPoseCity", 1)));
        Assert.Equal(7001, Single(planner, "c2500_0000", idle: 5000).IdleAnimation);
    }

    [Fact]
    public void Easy_asylum_uses_the_fixed_pool()
    {
        var patches = new MapPatchSettings
        {
            EasyAsylum = new EasyAsylumSettings { MapId = "m18_01_00_00", NormalEnemyIndices = [1], BossIndices = [3], Targets = ["c2500_0000"] },
        };
        var planner = Planner(new ScriptedRandom(1, 0, 0), Settings(("DifficultyMode", 3)), patches: patches);

        Assert.Equal("c2510", Single(planner, "c2500_0000", "m18_01_00_00").Enemy!.Id);
    }

    [Fact]
    public void Mimics_are_kept_when_mimic_replacement_is_off()
    {
        var planner = Planner(new ScriptedRandom(1), Settings(("MimicMode", 0)));
        Assert.Equal(ReplacementOutcome.MimicKept, Single(planner, "c2780_0000").Outcome);
    }

    [Fact]
    public void Difficulty_curve_picks_from_the_rolled_difficulty()
    {
        // Gwyndolin spot (difficulty 6), curve offset 0: bosses of difficulty 6 = [c5290, c5320]; Seath is banned
        var planner = Planner(new ScriptedRandom(1, 0, 0, 0), Settings(("DifficultyMode", 1)));
        Assert.Equal("c5320", Single(planner, "c5320_0000").Enemy!.Id);
    }

    [Fact]
    public void Difficulty_curve_without_fitting_enemy_reports_it()
    {
        // size 0 spot: no boss of difficulty 6 fits, and a boss spot may not become a normal enemy in "bosses only"
        var planner = Planner(new ScriptedRandom(1, 0), Settings(("DifficultyMode", 1), ("FitMode", 0)));
        Assert.Equal(ReplacementOutcome.NoFittingDifficulty, Single(planner, "c5320_0000", size: 0).Outcome);
    }

    [Fact]
    public void Same_seed_gives_the_same_plan()
    {
        IEnumerable<string?> Plan() => Planner(new SeededRandom("abc"))
            .PlanMap("m10", GameEdition.Ptde, [.. Enumerable.Range(0, 20).Select(i => new MapCreature($"c2500_{i:0000}", 5, -1))])
            .Decisions.Select(d => d.Enemy?.Id);

        Assert.Equal(Plan(), Plan());
    }
}
