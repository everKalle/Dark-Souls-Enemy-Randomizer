using EnemyRandomizer.Core.Settings;

namespace EnemyRandomizer.Core.Randomization;

/// <summary>Replace mode of bosses, normal enemies and NPCs (option values 0-3).</summary>
public enum ReplaceMode
{
    Off = 0,
    BossesOnly = 1,
    NormalsOnly = 2,
    Both = 3,
}

/// <summary>
/// Typed view of <see cref="RandomizerSettings"/> for one run. The only place that knows the option keys of
/// <c>randomizer-options.json</c> and what their values mean (legacy <c>randomizer_rng.py:1268-1276</c>).
/// </summary>
public sealed record RunOptions(RandomizerSettings Settings)
{
    public ReplaceMode BossMode => (ReplaceMode)Settings["BossMode"];
    public ReplaceMode NormalMode => (ReplaceMode)Settings["NormalMode"];
    public ReplaceMode NpcMode => (ReplaceMode)Settings["NpcMode"];
    public bool ReplaceMimics => Settings["MimicMode"] == 1;

    /// <summary>0 = only where they fit, 1 = anywhere, 2 = anywhere except NPC spots.</summary>
    public int FitMode => Settings["FitMode"];

    /// <summary>1 = curve, 2 = fully random, 3 = random + easy asylum, 4 = curve + easy asylum.</summary>
    public int DifficultyMode => Settings["DifficultyMode"];

    public bool UsesDifficultyCurve => DifficultyMode is 1 or 4;
    public bool EasyAsylum => DifficultyMode >= 3;
    public int ReplaceChance => Settings["ReplaceChance"];
    public int BossChanceNormals => Settings["BossChanceNormals"];
    public int BossChanceBosses => Settings["BossChanceBosses"];
    public int DifficultyStrictness => Settings["DiffStrictness"];
    public bool FixTPose => Settings["TPoseCity"] == 1;
    public int BossSoulDrops => Settings["BossSoulDrops"];
    public int PinwheelChaos => Settings["PinwheelChaos"];

    /// <summary>0 = on, 1 = off, 2 = on except roaming bosses.</summary>
    public bool TypeReplacement => Settings["TypeReplacement"] != 1;
    public bool TypeReplacementExceptBosses => Settings["TypeReplacement"] == 2;
    public bool PreventSame => Settings["PreventSame"] == 0;
    public bool UniqueBosses => Settings["UniqueBosses"] == 1;
    public bool DisableRoamingBossRespawn => Settings["RespawningBosses"] == 1;
    public bool HostileNpcs => Settings["HostileNpcs"] == 1;

    /// <summary>Raw value of any option (used by config conditions).</summary>
    public int Value(string key) => Settings.Values.TryGetValue(key, out var value) ? value : int.MinValue;
}
