namespace EnemyRandomizer.Core.Configuration;

/// <summary>
/// A per-map special case for original creatures (legacy <c>randomizer_rng.py:1426-1521, 1530, 1573-1576</c>).
/// Matches when the map is in <see cref="Maps"/> (empty = every map), the part name contains one of
/// <see cref="Parts"/>, contains none of <see cref="ExceptParts"/>, and <see cref="When"/> holds (null = always).
/// </summary>
public sealed class CreatureRule
{
    public string Name { get; set; } = "";
    public List<string> Maps { get; set; } = [];
    public List<string> Parts { get; set; } = [];
    public List<string> ExceptParts { get; set; } = [];
    public OptionCondition? When { get; set; }

    /// <summary>Never replace this creature.</summary>
    public bool Skip { get; set; }

    /// <summary>New position (x, y, z) for the replacement.</summary>
    public float[]? Position { get; set; }

    /// <summary>New rotation (x, y, z) for the replacement.</summary>
    public float[]? Rotation { get; set; }

    /// <summary>Treat the original as this type instead of its catalog type (Normal, Boss, Npc).</summary>
    public string? ReclassifyAs { get; set; }

    /// <summary>The replacement must fit the original spot even when "place anywhere" is on.</summary>
    public bool KeepOriginalSize { get; set; }

    /// <summary>With this chance (percent) the replacement is forced to <see cref="ForcedEnemyId"/>.</summary>
    public string? ForcedEnemyId { get; set; }

    public int ForcedChancePercent { get; set; }

    public bool Matches(string mapId, string partName, Func<string, int> optionValue)
    {
        ArgumentNullException.ThrowIfNull(partName);
        ArgumentNullException.ThrowIfNull(optionValue);
        return (Maps.Count == 0 || Maps.Contains(mapId))
               && Parts.Any(p => partName.Contains(p, StringComparison.Ordinal))
               && !ExceptParts.Any(p => partName.Contains(p, StringComparison.Ordinal))
               && (When is null || When.Values.Contains(optionValue(When.Option)));
    }

    public IEnumerable<string> Validate(RandomizerOptionSettings? options)
    {
        if (Parts.Count == 0)
        {
            yield return $"Creature rule '{Name}' needs at least one part.";
        }

        if (Position is { Length: not 3 } || Rotation is { Length: not 3 })
        {
            yield return $"Creature rule '{Name}': Position and Rotation need exactly 3 values.";
        }

        if (ReclassifyAs is not null && !Enum.TryParse<Data.EnemyType>(ReclassifyAs, out _))
        {
            yield return $"Creature rule '{Name}': unknown type '{ReclassifyAs}'.";
        }

        if (ForcedEnemyId is not null && ForcedChancePercent is < 0 or > 100)
        {
            yield return $"Creature rule '{Name}': ForcedChancePercent must be 0..100.";
        }

        if (When is not null && options is not null)
        {
            foreach (var error in When.Validate($"creature rule '{Name}'", options))
            {
                yield return error;
            }
        }
    }
}
