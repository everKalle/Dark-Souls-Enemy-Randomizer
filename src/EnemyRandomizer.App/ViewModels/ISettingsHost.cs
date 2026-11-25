using EnemyRandomizer.Core.Settings;

namespace EnemyRandomizer.App.ViewModels;

/// <summary>Read and replace the settings shown in the main window (used by the text config dialog).</summary>
public interface ISettingsHost
{
    RandomizerSettings Current { get; }

    void Apply(RandomizerSettings settings);
}
