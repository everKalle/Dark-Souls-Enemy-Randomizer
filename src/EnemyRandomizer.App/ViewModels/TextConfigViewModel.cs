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
    IAppShell shell) : ObservableObject
{
    public string HelpText => messages.Value.Get(MessageKeys.TextConfigHelp);

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ApplyCommand), nameof(CopyCommand))]
    public partial string ConfigText { get; set; } = "";

    /// <summary>Why the text could not be applied; shown next to the field instead of a message box.</summary>
    [ObservableProperty]
    public partial string ErrorText { get; set; } = "";

    [ObservableProperty]
    public partial string StatusText { get; set; } = "";

    partial void OnConfigTextChanged(string value)
    {
        ErrorText = "";
        StatusText = "";
    }

    [RelayCommand]
    private void Generate() => ConfigText = LegacyConfigString.Format(host.Current, options.Value);

    [RelayCommand(CanExecute = nameof(HasText))]
    private void Apply()
    {
        if (LegacyConfigString.TryParse(ConfigText, options.Value, host.Current.EnemyConfig, out var settings, out var error))
        {
            host.Apply(settings!);
            StatusText = messages.Value.Get(MessageKeys.TextConfigApplied);
        }
        else
        {
            ErrorText = messages.Value.Get(MessageKeys.TextConfigInvalid, error);
        }
    }

    [RelayCommand(CanExecute = nameof(HasText))]
    private void Copy()
    {
        shell.CopyText(ConfigText);
        StatusText = messages.Value.Get(MessageKeys.TextConfigCopied);
    }

    private bool HasText() => !string.IsNullOrWhiteSpace(ConfigText);
}
