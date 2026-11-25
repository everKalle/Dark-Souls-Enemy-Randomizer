namespace EnemyRandomizer.Core.Configuration;

/// <summary>
/// Rules that limit which enemy may replace which (<c>config/rules.json</c>, section <c>ReplacementRules</c>).
/// Legacy <c>randomizer_rng.py:1124-1207</c> (banned pairings, Gwyn/Lautrec nerf) and <c>:203, 1361-1367</c> (unique limits).
/// Per-map special cases (<c>:1427-1521</c>) are added in migration step 7 together with the code that uses them.
/// </summary>
public sealed class ReplacementRuleSettings : IValidatableSettings
{
    public static string SectionName => "ReplacementRules";

    public UniqueEnemyLimitSettings UniqueEnemyLimit { get; set; } = new();

    /// <summary>"Try for unique bosses" forgets the used bosses once this many were used.</summary>
    public int UniqueBossResetCount { get; set; }

    /// <summary>Enemy ids the "Enable All" button of the enemy config editor leaves untouched (legacy <c>randomizer.py:1309</c>).</summary>
    public List<string> ExcludedFromEnableAll { get; set; } = [];

    public List<BannedPairing> BannedPairings { get; set; } = [];

    public List<RandomRejection> RandomRejections { get; set; } = [];

    public IEnumerable<string> Validate()
    {
        var errors = new List<string>();
        errors.AddRange(UniqueEnemyLimit.Validate());
        errors.AddRange(ValidationHelpers.RequireRange(UniqueBossResetCount, 1, 1000, "ReplacementRules.UniqueBossResetCount"));

        foreach (var pairing in BannedPairings)
        {
            errors.AddRange(ValidationHelpers.RequireText(pairing.Reason, "Banned pairing reason"));
            if (pairing.Originals.Count == 0 || pairing.Replacements.Count == 0)
            {
                errors.Add($"Banned pairing '{pairing.Reason}' needs originals and replacements.");
            }
        }

        foreach (var rejection in RandomRejections)
        {
            errors.AddRange(ValidationHelpers.RequireText(rejection.ModelId, $"Random rejection '{rejection.Name}' model id"));
            var chances = rejection.ChanceByOptionValue.Values.Append(rejection.ChancePercent);
            if (chances.Any(c => c is < 0 or > 100))
            {
                errors.Add($"Random rejection '{rejection.Name}' chances must be 0..100.");
            }

            if (rejection.ChanceByOptionValue.Count > 0 && string.IsNullOrWhiteSpace(rejection.Option))
            {
                errors.Add($"Random rejection '{rejection.Name}' uses ChanceByOptionValue without Option.");
            }
        }

        return errors;
    }
}

/// <summary>Max. number of different enemy types per map; more picks reuse already used types.</summary>
public sealed class UniqueEnemyLimitSettings
{
    public int Default { get; set; }

    /// <summary>Map id → limit per edition.</summary>
    public Dictionary<string, Dictionary<GameEdition, int>> PerMap { get; set; } = [];

    public int For(string mapId, GameEdition edition) =>
        PerMap.TryGetValue(mapId, out var byEdition) && byEdition.TryGetValue(edition, out var limit)
            ? limit
            : Default;

    internal IEnumerable<string> Validate() =>
        ValidationHelpers.RequireRange(Default, 1, 1000, "UniqueEnemyLimit.Default")
            .Concat(PerMap.SelectMany(m => m.Value.SelectMany(e =>
                ValidationHelpers.RequireRange(e.Value, 1, 1000, $"UniqueEnemyLimit.PerMap.{m.Key}.{e.Key}"))));
}

/// <summary>
/// An original enemy whose id contains any of <see cref="Originals"/> must not be replaced by an enemy
/// whose id contains any of <see cref="Replacements"/> (substring match, like the legacy code).
/// </summary>
public sealed class BannedPairing
{
    public string Reason { get; set; } = "";
    public List<string> Originals { get; set; } = [];
    public List<string> Replacements { get; set; } = [];

    /// <summary>Only applies in the "easy asylum" modes.</summary>
    public bool OnlyWithEasyAsylum { get; set; }
}

/// <summary>A picked replacement with this model is thrown away with the given chance (spawn-rate nerf).</summary>
public sealed class RandomRejection
{
    public string Name { get; set; } = "";
    public string ModelId { get; set; } = "";

    /// <summary>Fixed chance in percent (used when <see cref="Option"/> is empty).</summary>
    public int ChancePercent { get; set; }

    /// <summary>Option whose value selects the chance from <see cref="ChanceByOptionValue"/>.</summary>
    public string Option { get; set; } = "";

    public Dictionary<int, int> ChanceByOptionValue { get; set; } = [];
}
