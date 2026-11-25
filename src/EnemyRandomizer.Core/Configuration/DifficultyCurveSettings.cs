namespace EnemyRandomizer.Core.Configuration;

/// <summary>
/// Chance tables for the "difficulty curve" mode (<c>config/difficulty-curves.json</c>, section <c>DifficultyCurves</c>).
/// Legacy <c>randomizer_rng.py:817-823</c>.
/// </summary>
public sealed class DifficultyCurveSettings : IValidatableSettings
{
    public static string SectionName => "DifficultyCurves";

    public int MinClass { get; set; }

    public int MaxClass { get; set; }

    /// <summary>Difficulty class offsets, tried in this order.</summary>
    public List<int> Offsets { get; set; } = [];

    /// <summary>One curve per strictness level (index = option value of <c>DiffStrictness</c>).</summary>
    public List<StrictnessCurve> Curves { get; set; } = [];

    public IEnumerable<string> Validate()
    {
        if (MinClass >= MaxClass)
        {
            yield return "DifficultyCurves: MinClass must be lower than MaxClass.";
        }

        if (Offsets.Count == 0)
        {
            yield return "DifficultyCurves.Offsets must not be empty.";
        }

        if (Curves.Count == 0)
        {
            yield return "DifficultyCurves.Curves must not be empty.";
        }

        foreach (var curve in Curves)
        {
            if (curve.Chances.Count != Offsets.Count)
            {
                yield return $"Curve '{curve.Name}' has {curve.Chances.Count} chances but there are {Offsets.Count} offsets.";
            }

            if (curve.Chances.Sum() != 100)
            {
                yield return $"Curve '{curve.Name}' chances add up to {curve.Chances.Sum()}, expected 100.";
            }

            if (curve.Chances.Any(c => c < 0))
            {
                yield return $"Curve '{curve.Name}' has negative chances.";
            }
        }
    }
}

public sealed class StrictnessCurve
{
    public string Name { get; set; } = "";

    /// <summary>Percent chance per entry of <see cref="DifficultyCurveSettings.Offsets"/>.</summary>
    public List<int> Chances { get; set; } = [];
}
