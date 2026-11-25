namespace EnemyRandomizer.Core.Data;

/// <summary>Kind of enemy in the catalog (column <c>Type</c>).</summary>
public enum EnemyType
{
    Normal = 0,
    Boss = 1,
    Npc = 2,
}

/// <summary>
/// One enemy that can be placed (row of <c>replacement_ref/valid_new.txt</c> or a custom enemy config).
/// <see cref="Ais"/> and <see cref="Params"/> are pairs: the replacement uses <c>Ais[i]</c> with <c>Params[i]</c>.
/// </summary>
public sealed record EnemyDefinition(
    int Index,
    string Id,
    string Name,
    EnemyType Type,
    string IgnoredFlag,
    int Size,
    int Difficulty,
    IReadOnlyList<string> Locations,
    IReadOnlyList<int> Ais,
    IReadOnlyList<int> Params,
    IReadOnlyList<int> IdleAnimations,
    string Comment)
{
    /// <summary><c>0</c> = enabled, <c>1</c> = disabled, <c>2</c> = enabled on Remastered only (Artorias).</summary>
    public bool IsEnabledOn(GameEdition edition) => IsEnabled(IgnoredFlag, edition);

    internal static bool IsEnabled(string ignoredFlag, GameEdition edition) => ignoredFlag switch
    {
        "0" => true,
        "2" => edition == GameEdition.Remastered,
        _ => false,
    };
}

/// <summary>
/// An original enemy type that may be replaced (row of <c>replacement_ref/valid_replacements.txt</c>).
/// Matched by substring against the part name, like v0.4.x.
/// </summary>
public sealed record ReplacementTarget(string Id, string Name, EnemyType Type, int Difficulty);

/// <summary>A creature row of a map (line of <c>original_enemies_ref/&lt;map&gt;.txt</c>, same order as the .msb).</summary>
public sealed record OriginalEnemy(string PartName, int Size);

/// <summary>A luainfo entry to add for an AI (<c>npcAi;primary;secondary;a;b;c;d</c>).</summary>
public sealed record AiInfoEntry(int NpcAi, string Primary, string Secondary, int FlagA, int FlagB, int FlagC, int FlagD);

/// <summary>Scripts and AI functions an enemy AI needs (row of <c>airef.csv</c>).</summary>
public sealed record AiReference(
    int NpcAi,
    string Description,
    string FilesName,
    string BattleScript,
    string LogicScript,
    IReadOnlyList<string> GnlFunctions,
    IReadOnlyList<AiInfoEntry> InfoEntries);
