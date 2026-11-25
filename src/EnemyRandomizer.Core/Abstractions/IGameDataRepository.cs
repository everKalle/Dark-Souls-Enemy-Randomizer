using EnemyRandomizer.Core.Data;

namespace EnemyRandomizer.Core.Abstractions;

/// <summary>Read access to the randomizer's data files in <c>enemyRandomizerData</c>.</summary>
public interface IGameDataRepository
{
    /// <summary>Enemy catalog of an enemy config (<c>Default</c> = <c>valid_new.txt</c>).</summary>
    IReadOnlyList<EnemyDefinition> LoadCatalog(string enemyConfig);

    /// <summary>Usable enemies for a run, grouped by kind, size and difficulty.</summary>
    EnemyPool LoadPool(string enemyConfig, GameEdition edition, bool hostileNpcs);

    ReplacementTargets ReplacementTargets { get; }

    /// <summary>Creature rows of a map in .msb order (edition specific file where one exists).</summary>
    IReadOnlyList<OriginalEnemy> LoadOriginalEnemies(string mapId, GameEdition edition);

    /// <summary>AI scripts and functions by NpcThinkParam id.</summary>
    IReadOnlyDictionary<int, AiReference> AiReferences { get; }
}
