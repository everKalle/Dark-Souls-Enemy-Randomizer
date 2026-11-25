namespace EnemyRandomizer.Core.Configuration;

/// <summary>
/// A settings object bound from one JSON section. Each implementation knows its section name and
/// validates itself, so one generic validator covers every settings type (DRY).
/// </summary>
public interface IValidatableSettings
{
    /// <summary>Name of the configuration section the settings are bound from.</summary>
    static abstract string SectionName { get; }

    /// <summary>Returns human readable problems; empty when the settings are valid.</summary>
    IEnumerable<string> Validate();
}
