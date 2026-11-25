namespace EnemyRandomizer.Core.Configuration;

/// <summary>
/// Character models added to every map's .msb model list during first-time setup
/// (<c>config/models.json</c>, section <c>MsbModels</c>). Legacy <c>randomizer_rng.py:512-553</c>.
/// </summary>
public sealed class MsbModelSettings : IValidatableSettings
{
    public static string SectionName => "MsbModels";

    public List<string> CharacterModels { get; set; } = [];

    /// <summary>Models of cut tails, appended after <see cref="CharacterModels"/>.</summary>
    public List<string> TailModels { get; set; } = [];

    /// <summary>SIB path format, <c>{0}</c> = model id.</summary>
    public string SibPathFormat { get; set; } = "";

    /// <summary>All models in the order they are appended.</summary>
    public IEnumerable<string> AllModels => CharacterModels.Concat(TailModels);

    public IEnumerable<string> Validate()
    {
        var errors = new List<string>();
        errors.AddRange(ValidationHelpers.RequireText(SibPathFormat, "MsbModels.SibPathFormat"));
        errors.AddRange(ValidationHelpers.Duplicates(AllModels, "model"));

        if (CharacterModels.Count == 0)
        {
            errors.Add("MsbModels.CharacterModels must not be empty.");
        }

        errors.AddRange(AllModels.Where(m => m.Length != 5 || m[0] != 'c' || !m[1..].All(char.IsAsciiDigit))
                                 .Select(m => $"Model id '{m}' must look like 'c1234'."));
        return errors;
    }
}
