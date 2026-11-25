using EnemyRandomizer.Core.Configuration;

namespace EnemyRandomizer.Core.Settings;

/// <summary>
/// The user's choices for one randomization run: a value per option key, the seed and the enemy config name.
/// Immutable; create changed copies with <c>with</c>.
/// </summary>
public sealed record RandomizerSettings
{
    /// <summary>Name of the built-in enemy config (<c>replacement_ref/valid_new.txt</c>).</summary>
    public const string DefaultEnemyConfig = "Default";

    public RandomizerSettings(IReadOnlyDictionary<string, int> values, string seed, string enemyConfig)
    {
        ArgumentNullException.ThrowIfNull(values);
        Values = new Dictionary<string, int>(values, StringComparer.OrdinalIgnoreCase);
        Seed = seed ?? "";
        EnemyConfig = string.IsNullOrWhiteSpace(enemyConfig) ? DefaultEnemyConfig : enemyConfig;
    }

    public IReadOnlyDictionary<string, int> Values { get; }

    public string Seed { get; init; }

    public string EnemyConfig { get; init; }

    /// <summary>Value of option <paramref name="key"/>.</summary>
    public int this[string key] => Values[key];

    /// <summary>All options on their default value, empty seed, default enemy config.</summary>
    public static RandomizerSettings CreateDefault(RandomizerOptionSettings options)
    {
        ArgumentNullException.ThrowIfNull(options);
        return new RandomizerSettings(options.Options.ToDictionary(o => o.Key, o => o.Default), "", DefaultEnemyConfig);
    }

    /// <summary>
    /// Returns a copy that has exactly the known options: unknown keys are dropped,
    /// missing or invalid values fall back to the option default.
    /// </summary>
    public RandomizerSettings Normalize(RandomizerOptionSettings options)
    {
        ArgumentNullException.ThrowIfNull(options);
        var values = options.Options.ToDictionary(
            o => o.Key,
            o => Values.TryGetValue(o.Key, out var v) && o.IsValidValue(v) ? v : o.Default);
        return new RandomizerSettings(values, Seed, EnemyConfig);
    }
}
