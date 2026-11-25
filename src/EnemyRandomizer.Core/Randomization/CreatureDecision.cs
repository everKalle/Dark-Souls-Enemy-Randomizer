using EnemyRandomizer.Core.Configuration;
using EnemyRandomizer.Core.Data;

namespace EnemyRandomizer.Core.Randomization;

/// <summary>An original creature of a map, as the planner needs it.</summary>
public sealed record MapCreature(string PartName, int Size, int IdleAnimation);

/// <summary>Why a creature was or was not replaced (log text of v0.4.x).</summary>
public enum ReplacementOutcome
{
    Replaced,

    /// <summary>Not an enemy the randomizer replaces (not in <c>valid_replacements.txt</c>).</summary>
    NotATarget,

    /// <summary>Excluded by a creature rule (special case).</summary>
    SpecialCase,

    /// <summary>Replace mode for this kind of enemy is "don't replace".</summary>
    ModeOff,

    /// <summary>Replacement chance roll failed.</summary>
    RandomChance,

    /// <summary>Mimic replacement is off.</summary>
    MimicKept,

    /// <summary>No candidate passed the rules.</summary>
    NoValidEnemy,

    /// <summary>The difficulty curve found no enemy that fits.</summary>
    NoFittingDifficulty,
}

/// <summary>What happens to one creature row of a map. Only <see cref="ReplacementOutcome.Replaced"/> rows change.</summary>
public sealed record CreatureDecision(int Row, string PartName, string OriginalName, ReplacementOutcome Outcome)
{
    public EnemyDefinition? Enemy { get; init; }

    /// <summary>The original is treated as this type (after reclassification).</summary>
    public EnemyType OriginalType { get; init; }

    public int Ai { get; init; }

    /// <summary>False for NPCs: their AI (talking etc.) is kept.</summary>
    public bool ChangeAi { get; init; }

    public int Param { get; init; }

    /// <summary>New idle animation, <c>null</c> = keep.</summary>
    public int? IdleAnimation { get; init; }

    public float[]? Position { get; init; }

    public float[]? Rotation { get; init; }

    public int? ItemLot { get; init; }

    /// <summary>Add a "stay dead" event (roaming boss respawn disabled).</summary>
    public bool DisableRespawn { get; init; }

    public TailCutDefinition? TailCut { get; init; }

    public int? Allegiance { get; init; }

    /// <summary>Events to replace by their template version.</summary>
    public IReadOnlyList<uint> EventFixes { get; init; } = [];
}

/// <summary>All decisions for one map.</summary>
public sealed record MapPlan(string MapId, int UniqueLimit, IReadOnlyList<CreatureDecision> Decisions);
