using EnemyRandomizer.Core.EnemyConfigs;
using EnemyRandomizer.Core.Settings;

namespace EnemyRandomizer.Core.Abstractions;

/// <summary>Remembers the settings of the last randomization.</summary>
public interface ILastConfigStore
{
    /// <summary>The saved settings (not yet normalized), or <c>null</c> when nothing was saved.</summary>
    RandomizerSettings? Load();

    void Save(RandomizerSettings settings);
}

/// <summary>Built-in and custom enemy configs (which enemies may be used as replacements).</summary>
public interface IEnemyConfigRepository
{
    /// <summary>Raised after a config was saved, so lists can refresh (observer).</summary>
    event EventHandler? ConfigsChanged;

    /// <summary><see cref="RandomizerSettings.DefaultEnemyConfig"/> first, then custom configs by name.</summary>
    IReadOnlyList<string> ListNames();

    IReadOnlyList<EnemyConfigEntry> Load(string name);

    bool Exists(string name);

    /// <summary>True for a name that can be used as a custom config file name.</summary>
    bool IsValidName(string name);

    /// <summary>Saves a custom config and returns the written file path.</summary>
    string Save(string name, IReadOnlyList<bool> enabled);
}
