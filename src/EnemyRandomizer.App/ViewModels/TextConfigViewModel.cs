using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EnemyRandomizer.App.Services;
using EnemyRandomizer.Core.Configuration;
using EnemyRandomizer.Core.Settings;
using Microsoft.Extensions.Options;

namespace EnemyRandomizer.App.ViewModels;

/// <summary>Text config dialog: export/import the shareable v0.4.x settings string.</summary>
public sealed partial class TextConfigViewModel(
    ISettingsHost host,
    IOptions<RandomizerOptionSettings> options,
    IOptions<MessageSettings> messages,
    IDialogService dialogs) : ObservableObject
{
    public string HelpText => messages.Value.Get(MessageKeys.TextConfigHelp);

    [ObservableProperty]
    public partial string ConfigText { get; set; } = "";

    [RelayCommand]
    private void Generate() => ConfigText = LegacyConfigString.Format(host.Current, options.Value);

    [RelayCommand]
    private void Apply()
    {
        if (LegacyConfigString.TryParse(ConfigText, options.Value, host.Current.EnemyConfig, out var settings, out var error))
        {
            host.Apply(settings!);
        }
        else
        {
            dialogs.ShowError("Text config", messages.Value.Get(MessageKeys.TextConfigInvalid, error));
        }
    }
}
