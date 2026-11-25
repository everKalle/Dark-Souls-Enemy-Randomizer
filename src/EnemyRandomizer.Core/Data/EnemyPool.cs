namespace EnemyRandomizer.Core.Data;

/// <summary>Which enemies a pool lookup returns.</summary>
public enum PoolKind
{
    Normal,
    Boss,
    Any,
}

/// <summary>
/// Catalog indices of the usable enemies, grouped by kind, size and difficulty (legacy <c>validSize*</c>,
/// <c>validDiff*</c>, <c>validDiffSize*</c>, <c>randomizer_rng.py:766-798</c>).
/// A size lookup returns every enemy of that size class <b>or smaller</b>: an enemy of size s fits every spot
/// of size s..5. NPCs count as normal enemies only when hostile NPCs are allowed.
/// </summary>
public sealed class EnemyPool
{
    /// <summary>Number of size classes (0 = tiny ... 5 = huge).</summary>
    public const int SizeClasses = 6;

    private readonly Dictionary<(PoolKind Kind, int Size), List<int>> _bySize = [];
    private readonly Dictionary<(PoolKind Kind, int Difficulty), List<int>> _byDifficulty = [];
    private readonly Dictionary<(PoolKind Kind, int Difficulty, int Size), List<int>> _byDifficultyAndSize = [];

    private EnemyPool(IReadOnlyList<EnemyDefinition> catalog, int difficultyClasses)
    {
        Catalog = catalog;
        DifficultyClasses = difficultyClasses;
    }

    public IReadOnlyList<EnemyDefinition> Catalog { get; }

    public int DifficultyClasses { get; }

    /// <summary>All enabled normal enemies (incl. hostile NPCs when allowed), in catalog order.</summary>
    public IReadOnlyList<int> Normals => BySize(PoolKind.Normal, SizeClasses - 1);

    /// <summary>All enabled bosses, in catalog order.</summary>
    public IReadOnlyList<int> Bosses => BySize(PoolKind.Boss, SizeClasses - 1);

    public static EnemyPool Build(IReadOnlyList<EnemyDefinition> catalog, GameEdition edition, bool hostileNpcs, int maxDifficulty)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        var pool = new EnemyPool(catalog, maxDifficulty + 1);
        foreach (var enemy in catalog.Where(e => e.IsEnabledOn(edition)))
        {
            var kind = KindOf(enemy, hostileNpcs);
            var kinds = kind is null ? new[] { PoolKind.Any } : [PoolKind.Any, kind.Value];
            foreach (var k in kinds)
            {
                Add(pool._byDifficulty, (k, enemy.Difficulty), enemy.Index);
                for (var size = enemy.Size; size < SizeClasses; size++)
                {
                    Add(pool._bySize, (k, size), enemy.Index);
                    Add(pool._byDifficultyAndSize, (k, enemy.Difficulty, size), enemy.Index);
                }
            }
        }

        return pool;
    }

    /// <summary>Enemies that fit a spot of size <paramref name="maxSize"/>.</summary>
    public IReadOnlyList<int> BySize(PoolKind kind, int maxSize) => Get(_bySize, (kind, Clamp(maxSize)));

    /// <summary>Enemies of exactly this difficulty, any size.</summary>
    public IReadOnlyList<int> ByDifficulty(PoolKind kind, int difficulty) => Get(_byDifficulty, (kind, difficulty));

    /// <summary>Enemies of exactly this difficulty that fit a spot of size <paramref name="maxSize"/>.</summary>
    public IReadOnlyList<int> ByDifficultyAndSize(PoolKind kind, int difficulty, int maxSize) =>
        Get(_byDifficultyAndSize, (kind, difficulty, Clamp(maxSize)));

    public EnemyDefinition this[int index] => Catalog[index];

    /// <summary>NPCs (type 2) are normal enemies only with hostile NPCs on; otherwise only in the "any" pool.</summary>
    private static PoolKind? KindOf(EnemyDefinition enemy, bool hostileNpcs) => enemy.Type switch
    {
        EnemyType.Normal => PoolKind.Normal,
        EnemyType.Boss => PoolKind.Boss,
        EnemyType.Npc when hostileNpcs => PoolKind.Normal,
        _ => null,
    };

    private static int Clamp(int size) => Math.Clamp(size, 0, SizeClasses - 1);

    private static void Add<TKey>(Dictionary<TKey, List<int>> map, TKey key, int index)
        where TKey : notnull
    {
        if (!map.TryGetValue(key, out var list))
        {
            list = [];
            map[key] = list;
        }

        list.Add(index);
    }

    private static List<int> Get<TKey>(Dictionary<TKey, List<int>> map, TKey key)
        where TKey : notnull =>
        map.TryGetValue(key, out var list) ? list : [];
}

/// <summary>Finds the replacement target for a part name (first entry whose id is contained, legacy <c>validIndex</c>).</summary>
public sealed class ReplacementTargets(IReadOnlyList<ReplacementTarget> targets)
{
    public IReadOnlyList<ReplacementTarget> All { get; } = targets;

    public ReplacementTarget? Find(string partName)
    {
        ArgumentNullException.ThrowIfNull(partName);
        return All.FirstOrDefault(t => partName.Contains(t.Id, StringComparison.Ordinal));
    }
}
