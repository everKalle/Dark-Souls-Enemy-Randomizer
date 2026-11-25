using EnemyRandomizer.Core.Configuration;
using EnemyRandomizer.Core.Data;

namespace EnemyRandomizer.Core.Randomization;

/// <summary>A rule that can forbid replacing an original part by a candidate (legacy <c>isCombinationInvalid</c>).</summary>
public interface IReplacementRule
{
    bool Rejects(string originalPart, EnemyDefinition candidate);
}

/// <summary>Composite: rejects when any child rule rejects (children run in order, like the legacy checks).</summary>
public sealed class CompositeReplacementRule(IEnumerable<IReplacementRule> rules) : IReplacementRule
{
    private readonly IReadOnlyList<IReplacementRule> _rules = rules.ToList();

    public bool Rejects(string originalPart, EnemyDefinition candidate) => _rules.Any(r => r.Rejects(originalPart, candidate));
}

/// <summary>Banned original/replacement pairs from <c>rules.json</c> (substring match like v0.4.x).</summary>
public sealed class BannedPairingRule(IEnumerable<BannedPairing> pairings, bool easyAsylum) : IReplacementRule
{
    private readonly IReadOnlyList<BannedPairing> _pairings = pairings.Where(p => easyAsylum || !p.OnlyWithEasyAsylum).ToList();

    public bool Rejects(string originalPart, EnemyDefinition candidate) =>
        _pairings.Any(p => p.Originals.Any(o => originalPart.Contains(o, StringComparison.Ordinal))
                           && p.Replacements.Any(r => candidate.Id.Contains(r, StringComparison.Ordinal)));
}

/// <summary>Spawn-rate nerfs: a candidate with this model is thrown away with a chance (Gwyn, Lautrec).</summary>
public sealed class RandomRejectionRule(IEnumerable<RandomRejection> rejections, RunOptions options, IRandomSource random) : IReplacementRule
{
    private readonly IReadOnlyList<(string ModelId, int Chance)> _rejections = rejections
        .Select(r => (r.ModelId, string.IsNullOrEmpty(r.Option)
            ? r.ChancePercent
            : r.ChanceByOptionValue.GetValueOrDefault(options.Value(r.Option))))
        .Where(r => r.Item2 > 0)
        .ToList();

    public bool Rejects(string originalPart, EnemyDefinition candidate) =>
        _rejections.Any(r => candidate.Id.Contains(r.ModelId, StringComparison.Ordinal) && random.NextPercent() < r.Chance);
}

/// <summary>"Prevent replacement with same enemy".</summary>
public sealed class PreventSameRule : IReplacementRule
{
    public bool Rejects(string originalPart, EnemyDefinition candidate) =>
        originalPart.Contains(candidate.Id, StringComparison.Ordinal);
}

/// <summary>
/// With type replacement, one map must not use the same enemy for two different original types.
/// <paramref name="usedTypes"/> returns the enemies already chosen for types in the current map.
/// </summary>
public sealed class DistinctTypeReplacementRule(Func<IEnumerable<EnemyDefinition>> usedTypes) : IReplacementRule
{
    public bool Rejects(string originalPart, EnemyDefinition candidate) =>
        usedTypes().Any(used => candidate.Id.Contains(used.Id, StringComparison.Ordinal));
}
