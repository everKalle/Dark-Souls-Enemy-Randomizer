namespace EnemyRandomizer.Core.Configuration;

/// <summary>
/// Every user option of the randomizer: labels, descriptions, defaults, ranges and when an option has an effect
/// (<c>config/randomizer-options.json</c>, section <c>RandomizerOptions</c>). The UI, the text config import/export
/// and validation are all driven from this single definition (legacy <c>randomizer.py:123-621, 697-729, 1500-1635</c>).
/// </summary>
public sealed class RandomizerOptionSettings : IValidatableSettings
{
    public static string SectionName => "RandomizerOptions";

    public List<OptionTab> Tabs { get; set; } = [];

    /// <summary>Options in display order (per tab).</summary>
    public List<OptionDefinition> Options { get; set; } = [];

    public OptionDefinition? Find(string key) =>
        Options.FirstOrDefault(o => string.Equals(o.Key, key, StringComparison.OrdinalIgnoreCase));

    public IEnumerable<string> Validate()
    {
        var errors = new List<string>();
        errors.AddRange(ValidationHelpers.Duplicates(Tabs.Select(t => t.Id), "tab id"));
        errors.AddRange(ValidationHelpers.Duplicates(Options.Select(o => o.Key), "option key"));
        errors.AddRange(ValidationHelpers.Duplicates(Options.Select(o => o.LegacyIndex), "option legacy index"));

        var expectedIndices = Enumerable.Range(0, Options.Count);
        if (!Options.Select(o => o.LegacyIndex).Order().SequenceEqual(expectedIndices))
        {
            errors.Add($"Option legacy indices must be 0..{Options.Count - 1} without gaps.");
        }

        var tabIds = Tabs.Select(t => t.Id).ToHashSet();
        foreach (var option in Options)
        {
            if (!tabIds.Contains(option.Tab))
            {
                errors.Add($"Option '{option.Key}' uses unknown tab '{option.Tab}'.");
            }

            errors.AddRange(option.Validate());
            errors.AddRange(option.EnabledWhen.SelectMany(c => c.Validate(option.Key, this)));
        }

        return errors;
    }
}

public sealed class OptionTab
{
    public string Id { get; set; } = "";
    public string Title { get; set; } = "";
}

public enum OptionKind
{
    /// <summary>Radio button group.</summary>
    Choice,

    /// <summary>Integer slider (percentages).</summary>
    Slider,
}

public sealed class OptionDefinition
{
    /// <summary>Stable key used in code and in <c>lastConfig.json</c>.</summary>
    public string Key { get; set; } = "";

    /// <summary>Position in the legacy <c>/-/</c> separated text config.</summary>
    public int LegacyIndex { get; set; }

    public string Tab { get; set; } = "";

    public OptionKind Kind { get; set; }

    public string Title { get; set; } = "";

    /// <summary>Help text for sliders (choices carry their own description).</summary>
    public string Description { get; set; } = "";

    public int Default { get; set; }

    /// <summary>Slider minimum (choices: derived from values).</summary>
    public int Min { get; set; }

    /// <summary>Slider maximum (choices: derived from values).</summary>
    public int Max { get; set; }

    /// <summary>Radio buttons in display order.</summary>
    public List<OptionChoice> Choices { get; set; } = [];

    /// <summary>The option only has an effect when ANY of these conditions holds. Empty = always.</summary>
    public List<OptionCondition> EnabledWhen { get; set; } = [];

    /// <summary>True when <paramref name="value"/> is allowed for this option.</summary>
    public bool IsValidValue(int value) =>
        Kind == OptionKind.Choice
            ? Choices.Any(c => c.Value == value)
            : value >= Min && value <= Max;

    internal IEnumerable<string> Validate()
    {
        var errors = new List<string>();
        errors.AddRange(ValidationHelpers.RequireText(Key, "Option key"));
        errors.AddRange(ValidationHelpers.RequireText(Title, $"Option '{Key}' title"));

        if (Kind == OptionKind.Choice)
        {
            if (Choices.Count < 2)
            {
                errors.Add($"Choice option '{Key}' needs at least two choices.");
            }

            errors.AddRange(ValidationHelpers.Duplicates(Choices.Select(c => c.Value), $"value in option '{Key}'"));
            errors.AddRange(Choices.SelectMany(c => ValidationHelpers.RequireText(c.Label, $"Option '{Key}' choice {c.Value} label")));
        }
        else
        {
            if (Min > Max)
            {
                errors.Add($"Slider option '{Key}' has Min > Max.");
            }

            if (Choices.Count != 0)
            {
                errors.Add($"Slider option '{Key}' must not have choices.");
            }
        }

        if (!IsValidValue(Default))
        {
            errors.Add($"Option '{Key}' default {Default} is not an allowed value.");
        }

        return errors;
    }
}

public sealed class OptionChoice
{
    public int Value { get; set; }
    public string Label { get; set; } = "";
    public string Description { get; set; } = "";
}

/// <summary>Holds when option <see cref="Option"/> has one of <see cref="Values"/>.</summary>
public sealed class OptionCondition
{
    public string Option { get; set; } = "";
    public List<int> Values { get; set; } = [];

    internal IEnumerable<string> Validate(string ownerKey, RandomizerOptionSettings settings)
    {
        var target = settings.Find(Option);
        if (target is null)
        {
            yield return $"Option '{ownerKey}' depends on unknown option '{Option}'.";
            yield break;
        }

        if (Values.Count == 0)
        {
            yield return $"Option '{ownerKey}' condition on '{Option}' has no values.";
        }

        foreach (var value in Values.Where(v => !target.IsValidValue(v)))
        {
            yield return $"Option '{ownerKey}' condition uses invalid value {value} of '{Option}'.";
        }
    }
}
