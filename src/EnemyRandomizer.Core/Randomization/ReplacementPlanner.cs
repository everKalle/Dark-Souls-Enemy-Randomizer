using EnemyRandomizer.Core.Configuration;
using EnemyRandomizer.Core.Data;

namespace EnemyRandomizer.Core.Randomization;

/// <summary>Configuration the planner reads (grouped to keep the constructor small).</summary>
public sealed record PlannerSettings(
    ReplacementRuleSettings Rules,
    MapPatchSettings Patches,
    DifficultyCurveSettings Curves,
    string MimicModelId);

/// <summary>
/// Decides, map by map, which enemy replaces which original creature (legacy <c>randomize</c> loop and
/// <c>GetNormalEnemy</c>/<c>GetBossEnemy</c>/<c>GetEnemyFromListWithRetry</c>/<c>getDifficultyList</c>,
/// <c>randomizer_rng.py:817-991, 1417-1741</c>). Pure logic: reads no files and writes nothing; the result is
/// applied to the game files by the infrastructure. One planner instance = one randomization run.
/// </summary>
public sealed class ReplacementPlanner
{
    private const int NoEnemy = -4;
    private const int NoDifficultyMatch = -6;
    private const int FullyRandomMode = 2;

    private readonly EnemyPool _pool;
    private readonly ReplacementTargets _targets;
    private readonly RunOptions _options;
    private readonly PlannerSettings _settings;
    private readonly IRandomSource _random;
    private readonly CompositeReplacementRule _rules;
    private readonly List<int> _usedBosses = [];   // run-wide, for "try for unique bosses"
    private MapState _map = new();

    public ReplacementPlanner(EnemyPool pool, ReplacementTargets targets, RunOptions options, PlannerSettings settings, IRandomSource random)
    {
        ArgumentNullException.ThrowIfNull(settings);
        _pool = pool;
        _targets = targets;
        _options = options;
        _settings = settings;
        _random = random;

        var rules = new List<IReplacementRule> { new BannedPairingRule(settings.Rules.BannedPairings, options.EasyAsylum) };
        if (options.TypeReplacement)
        {
            rules.Add(new DistinctTypeReplacementRule(() => _map.TypeMap.Values.Where(i => i >= 0).Select(i => _pool[i])));
        }

        rules.Add(new RandomRejectionRule(settings.Rules.RandomRejections, options, random));
        if (options.PreventSame)
        {
            rules.Add(new PreventSameRule());
        }

        _rules = new CompositeReplacementRule(rules);
    }

    public MapPlan PlanMap(string mapId, GameEdition edition, IReadOnlyList<MapCreature> creatures)
    {
        ArgumentNullException.ThrowIfNull(creatures);
        _map = new MapState(mapId, _settings.Rules.UniqueEnemyLimit.For(mapId, edition));
        var decisions = creatures.Select((c, row) => Decide(row, c, edition)).ToList();
        return new MapPlan(mapId, _map.UniqueLimit, decisions);
    }

    private CreatureDecision Decide(int row, MapCreature creature, GameEdition edition)
    {
        var part = creature.PartName;
        var target = _targets.Find(part);
        if (target is null)
        {
            return new CreatureDecision(row, part, "", ReplacementOutcome.NotATarget);
        }

        var rules = _settings.Rules.CreatureRules.Where(r => r.Matches(_map.MapId, part, _options.Value)).ToList();

        if (rules.Any(r => r.Skip))
        {
            return new CreatureDecision(row, part, target.Name, ReplacementOutcome.SpecialCase);
        }

        var type = ReclassifiedType(rules) ?? target.Type;
        var typeKey = part.Split('_')[0];
        var chosen = Choose(part, creature.Size, target, type, rules);

        chosen = ApplyTypeReplacement(typeKey, type, chosen);
        if (chosen < 0)
        {
            return new CreatureDecision(row, part, target.Name, Outcome(chosen)) { OriginalType = type };
        }

        var enemy = _pool[chosen];
        if (type != EnemyType.Boss && !_map.UsedEnemies.Contains(chosen))
        {
            _map.UsedEnemies.Add(chosen);
        }

        var pair = enemy.Ais.Count == 1 ? 0 : _random.Between(0, enemy.Ais.Count - 1);
        var param = enemy.Params[pair];
        if (type == EnemyType.Normal && enemy.Type == EnemyType.Boss && !_settings.Rules.NoSoulVariant.Contains(enemy.Id))
        {
            param += 50; // "with souls" NpcParam copy
        }

        var keepsOwnPlace = part.Contains("c2232", StringComparison.Ordinal) && enemy.Id.Contains("c2232", StringComparison.Ordinal);
        var positionRule = keepsOwnPlace ? null : rules.FirstOrDefault(r => r.Position is not null);
        var newPosition = Lookup(_settings.Patches.AmbushPositions, part) ?? Lookup(_settings.Patches.OtherPositions, part);

        return Describe(row, part, target, type, enemy, edition) with
        {
            Ai = enemy.Ais[pair],
            ChangeAi = target.Type != EnemyType.Npc,
            Param = param,
            IdleAnimation = _options.FixTPose && type != EnemyType.Npc && creature.IdleAnimation != -1
                ? Pick(enemy.IdleAnimations, -1)
                : null,
            Position = newPosition ?? positionRule?.Position,
            Rotation = positionRule?.Rotation,
        };
    }

    /// <summary>
    /// Replays a saved layout (legacy <c>CopyFromReference</c>, <c>randomizer_rng.py:1957-2055</c>): creatures listed
    /// in <paramref name="layout"/> get exactly the saved model, AI, param, entity id, animation and position; the
    /// follow-up changes (item lots, tails, allegiances, event fixes) are derived like in a normal run.
    /// No random numbers are used.
    /// </summary>
    public MapPlan PlanMapFromLayout(
        string mapId, GameEdition edition, IReadOnlyList<MapCreature> creatures, MapLayout layout, int modelStartIndex)
    {
        ArgumentNullException.ThrowIfNull(creatures);
        ArgumentNullException.ThrowIfNull(layout);
        _map = new MapState(mapId);
        var decisions = creatures.Select((c, row) => FromLayout(row, c.PartName, edition, layout, modelStartIndex)).ToList();
        return new MapPlan(mapId, _map.UniqueLimit, decisions);
    }

    private CreatureDecision FromLayout(int row, string part, GameEdition edition, MapLayout layout, int modelStartIndex)
    {
        var target = _targets.Find(part);
        if (!layout.Rows.TryGetValue(part, out var saved))
        {
            return new CreatureDecision(row, part, target?.Name ?? "", ReplacementOutcome.NotATarget);
        }

        var index = saved.Model - modelStartIndex;
        if (target is null || index < 0 || index >= _pool.Catalog.Count)
        {
            throw new InvalidDataException(
                $"{_map.MapId}: '{part}' cannot be restored (model {saved.Model} is not an enemy of this game edition).");
        }

        var enemy = _pool[index];
        var rules = _settings.Rules.CreatureRules.Where(r => r.Matches(_map.MapId, part, _options.Value)).ToList();
        var type = ReclassifiedType(rules) ?? target.Type;

        return Describe(row, part, target, type, enemy, edition) with
        {
            Ai = saved.Ai,
            ChangeAi = true,
            Param = saved.Param,
            IdleAnimation = saved.AnimId,
            Position = [saved.X, saved.Y, saved.Z],
            Rotation = [saved.RotX, saved.RotY, saved.RotZ],
            EntityId = saved.EntityId,
            FromLayout = true,
        };
    }

    /// <summary>The parts of a decision that depend only on the creature and the new enemy, not on a random pick.</summary>
    private CreatureDecision Describe(int row, string part, ReplacementTarget target, EnemyType type, EnemyDefinition enemy, GameEdition edition)
    {
        var patches = _settings.Patches;
        return new CreatureDecision(row, part, target.Name, ReplacementOutcome.Replaced)
        {
            Enemy = enemy,
            OriginalType = type,
            ItemLot = patches.ItemLotsToAward.TryGetValue(_map.MapId, out var lots) && lots.TryGetValue(part, out var lot) ? lot : null,
            DisableRespawn = _options.DisableRoamingBossRespawn && enemy.Type == EnemyType.Boss && type == EnemyType.Normal,
            TailCut = patches.TailCutsAllowed(edition, _map.MapId) ? patches.TailCuts.FirstOrDefault(t => t.ModelId == enemy.Id) : null,
            Allegiance = patches.CharacterAllegiances.TryGetValue(enemy.Id, out var team) ? team : null,
            EventFixes = patches.EventFixes
                .Where(f => f.Maps.Contains(_map.MapId) && part.Contains(f.Part, StringComparison.Ordinal)
                            && !enemy.Id.Contains(f.UnlessReplacementContains, StringComparison.Ordinal))
                .SelectMany(f => f.TemplateEventIds)
                .ToList(),
        };
    }

    private static EnemyType? ReclassifiedType(IEnumerable<CreatureRule> rules) =>
        rules.Select(r => r.ReclassifyAs).OfType<string>().Select(Enum.Parse<EnemyType>).Cast<EnemyType?>().LastOrDefault();

    /// <summary>Chooses the replacement index (or a negative outcome code) for one original creature.</summary>
    private int Choose(string part, int size, ReplacementTarget target, EnemyType type, IReadOnlyList<CreatureRule> rules)
    {
        if (_random.Between(1, 100) > _options.ReplaceChance)
        {
            return Code(ReplacementOutcome.RandomChance);
        }

        var maxSize = _options.FitMode == 0 || rules.Any(r => r.KeepOriginalSize) ? size : EnemyPool.SizeClasses - 1;
        var difficulty = target.Difficulty;
        var mode = _options.DifficultyMode;

        switch (type)
        {
            case EnemyType.Normal when _options.NormalMode != ReplaceMode.Off:
                if (part.Contains(_settings.MimicModelId, StringComparison.Ordinal) && !_options.ReplaceMimics)
                {
                    return Code(ReplacementOutcome.MimicKept);
                }

                return _options.NormalMode switch
                {
                    ReplaceMode.BossesOnly => Boss(mode, true, maxSize, difficulty, part, canBeNormal: false, replacingBoss: false),
                    ReplaceMode.NormalsOnly => Normal(mode, true, maxSize, difficulty, part),
                    _ => NormalOrBoss(mode, _options.BossChanceNormals, true, maxSize, difficulty, part, replacingBoss: false),
                };

            case EnemyType.Boss when _options.BossMode != ReplaceMode.Off:
                var boss = _options.BossMode switch
                {
                    ReplaceMode.BossesOnly => Boss(mode, false, maxSize, difficulty, part, canBeNormal: false, replacingBoss: true),
                    ReplaceMode.NormalsOnly => Normal(mode, false, maxSize, difficulty, part),
                    _ => NormalOrBoss(mode, _options.BossChanceBosses, false, maxSize, difficulty, part, replacingBoss: true),
                };
                if (boss >= 0 && _pool[boss].Type == EnemyType.Boss && !_usedBosses.Contains(boss))
                {
                    _usedBosses.Add(boss);
                }

                return boss;

            case EnemyType.Npc when _options.NpcMode != ReplaceMode.Off:
                if (_options.FitMode == 2)
                {
                    maxSize = size;
                }

                var npc = _options.NpcMode switch
                {
                    ReplaceMode.BossesOnly => Boss(FullyRandomMode, true, maxSize, difficulty, part, canBeNormal: false, replacingBoss: false),
                    ReplaceMode.NormalsOnly => Normal(FullyRandomMode, true, maxSize, difficulty, part),
                    _ => NormalOrBoss(FullyRandomMode, _options.BossChanceNormals, true, maxSize, difficulty, part, replacingBoss: false),
                };
                return Forced(rules) ?? npc;

            default:
                return Code(ReplacementOutcome.ModeOff);
        }
    }

    /// <summary>Andre may become Gwyndolin (creature rule with a forced enemy).</summary>
    private int? Forced(IReadOnlyList<CreatureRule> rules)
    {
        var rule = rules.FirstOrDefault(r => r.ForcedEnemyId is not null);
        if (rule is null || _random.Between(1, 100) > rule.ForcedChancePercent)
        {
            return null;
        }

        var enemy = _pool.Catalog.FirstOrDefault(e => e.Id == rule.ForcedEnemyId);
        return enemy?.Index;
    }

    /// <summary>
    /// Type replacement: the first replacement of an original type is remembered and reused for every later
    /// creature of that type in the map (also "not replaced"); bosses always pick anew (legacy <c>:1581-1592</c>).
    /// </summary>
    private int ApplyTypeReplacement(string typeKey, EnemyType type, int chosen)
    {
        if (!_options.TypeReplacement)
        {
            return chosen;
        }

        var exceptBosses = _options.TypeReplacementExceptBosses;
        if (_map.TypeMap.TryGetValue(typeKey, out var remembered) && type != EnemyType.Boss)
        {
            return exceptBosses && IsBoss(chosen) ? chosen : remembered;
        }

        if (!exceptBosses || !IsBoss(chosen))
        {
            _map.TypeMap[typeKey] = chosen;
        }

        return chosen;
    }

    private int Normal(int mode, bool careAboutLimit, int maxSize, int difficulty, string part)
    {
        int chosen;
        if (!careAboutLimit || _map.UsedEnemies.Count < _map.UniqueLimit)
        {
            if (mode is 1 or 4)
            {
                var list = DifficultyList(difficulty, PoolKind.Normal, maxSize);
                chosen = list.Count > 0 ? PickWithRetry(list, part, isBoss: false) : NoDifficultyMatch;
            }
            else
            {
                chosen = PickWithRetry(_pool.BySize(PoolKind.Normal, maxSize), part, isBoss: false);
            }
        }
        else
        {
            chosen = PickWithRetry(Used(EnemyType.Normal, maxSize), part, isBoss: false);
        }

        return EasyAsylum(mode, part, _settings.Patches.EasyAsylum.NormalEnemyIndices) ?? chosen;
    }

    private int Boss(int mode, bool careAboutLimit, int maxSize, int difficulty, string part, bool canBeNormal, bool replacingBoss)
    {
        int chosen;
        if (!careAboutLimit || _map.UsedEnemies.Count < _map.UniqueLimit)
        {
            if (mode is 1 or 4)
            {
                var list = DifficultyList(difficulty, PoolKind.Boss, maxSize);
                if (list.Count == 0 && canBeNormal)
                {
                    return Normal(mode, careAboutLimit, maxSize, difficulty, part);
                }

                chosen = list.Count > 0 ? PickWithRetry(list, part, replacingBoss) : NoDifficultyMatch;
            }
            else
            {
                chosen = PickWithRetry(_pool.BySize(PoolKind.Boss, maxSize), part, replacingBoss);
            }
        }
        else
        {
            var used = Used(EnemyType.Boss, maxSize);
            if (used.Count == 0 && canBeNormal)
            {
                return Normal(mode, careAboutLimit, maxSize, difficulty, part);
            }

            chosen = PickWithRetry(used, part, isBoss: false);
        }

        return EasyAsylum(mode, part, _settings.Patches.EasyAsylum.BossIndices) ?? chosen;
    }

    private int NormalOrBoss(int mode, int bossChance, bool careAboutLimit, int maxSize, int difficulty, string part, bool replacingBoss) =>
        _random.Between(1, 100) <= bossChance
            ? Boss(mode, careAboutLimit, maxSize, difficulty, part, canBeNormal: true, replacingBoss)
            : Normal(mode, careAboutLimit, maxSize, difficulty, part);

    /// <summary>Easy asylum modes use fixed pools for the asylum's targets.</summary>
    private int? EasyAsylum(int mode, string part, List<int> indices)
    {
        var asylum = _settings.Patches.EasyAsylum;
        return mode >= 3 && _map.MapId == asylum.MapId && asylum.Targets.Contains(part) && indices.Count > 0
            ? indices[_random.Between(0, indices.Count - 1)]
            : null;
    }

    /// <summary>Random candidates until one passes the rules; unique bosses skip bosses used before.</summary>
    private int PickWithRetry(IReadOnlyList<int> candidates, string part, bool isBoss)
    {
        var list = candidates.ToList();
        var resetCount = _settings.Rules.UniqueBossResetCount;
        while (list.Count > 0)
        {
            var index = _random.Between(0, list.Count - 1);
            var candidate = list[index];
            if (!_rules.Rejects(part, _pool[candidate]))
            {
                if (!isBoss || !_options.UniqueBosses)
                {
                    return candidate;
                }

                if (!_usedBosses.Contains(candidate) || _usedBosses.Count == resetCount || list.Count == 1)
                {
                    if (_usedBosses.Count == resetCount)
                    {
                        _usedBosses.Clear();
                    }

                    return candidate;
                }
            }

            list.RemoveAt(index);
        }

        return NoEnemy;
    }

    /// <summary>
    /// Difficulty curve: rolls an offset from the strictness table around the original's difficulty; falls back
    /// to the original difficulty when the rolled class has no fitting enemy (legacy <c>getDifficultyList</c>).
    /// </summary>
    private IReadOnlyList<int> DifficultyList(int desired, PoolKind kind, int maxSize)
    {
        var curves = _settings.Curves;
        var chances = curves.Curves[Math.Clamp(_options.DifficultyStrictness, 0, curves.Curves.Count - 1)].Chances;
        var roll = _random.Between(0, 99); // 100 outcomes: chances add up to at most 100, so 100 would never match
        var sum = 0;
        for (var i = 0; i < chances.Count; i++)
        {
            var difficulty = desired + curves.Offsets[i];
            if (difficulty < curves.MinClass || difficulty > curves.MaxClass || chances[i] <= 0)
            {
                continue;
            }

            sum += chances[i];
            if (sum > roll)
            {
                var list = _pool.ByDifficultyAndSize(kind, difficulty, maxSize);
                return list.Count > 0 ? list : _pool.ByDifficultyAndSize(kind, desired, maxSize);
            }
        }

        return _pool.ByDifficultyAndSize(kind, desired, maxSize);
    }

    /// <summary>Enemies already used in this map that fit (once the unique limit is reached).</summary>
    private List<int> Used(EnemyType type, int maxSize) =>
        [.. _map.UsedEnemies.Where(i => (_pool[i].Type == EnemyType.Boss) == (type == EnemyType.Boss) && _pool[i].Size <= maxSize)];

    private bool IsBoss(int index) => index >= 0 && _pool[index].Type == EnemyType.Boss;

    private int Pick(IReadOnlyList<int> values, int fallback) =>
        values.Count == 0 ? fallback : values[_random.Between(0, values.Count - 1)];

    private float[]? Lookup(Dictionary<string, Dictionary<string, float[]>> positions, string part) =>
        positions.TryGetValue(_map.MapId, out var parts) && parts.TryGetValue(part, out var position) ? position : null;

    /// <summary>Negative code for "not replaced" outcomes, kept apart from the legacy -4/-6 codes.</summary>
    private static int Code(ReplacementOutcome outcome) => -100 - (int)outcome;

    private static ReplacementOutcome Outcome(int code) => code switch
    {
        NoEnemy => ReplacementOutcome.NoValidEnemy,
        NoDifficultyMatch => ReplacementOutcome.NoFittingDifficulty,
        <= -100 => (ReplacementOutcome)(-100 - code),
        _ => ReplacementOutcome.NoValidEnemy,
    };

    /// <summary>State that resets per map.</summary>
    private sealed class MapState(string mapId = "", int uniqueLimit = int.MaxValue)
    {
        public string MapId { get; } = mapId;
        public int UniqueLimit { get; } = uniqueLimit;

        /// <summary>Distinct enemies placed on non-boss spots, in order (legacy <c>uniqueIndices</c>).</summary>
        public List<int> UsedEnemies { get; } = [];

        /// <summary>Original type id (e.g. <c>c2500</c>) → chosen index or negative outcome code.</summary>
        public Dictionary<string, int> TypeMap { get; } = new(StringComparer.Ordinal);
    }
}
