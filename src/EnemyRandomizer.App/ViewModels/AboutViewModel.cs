using CommunityToolkit.Mvvm.Input;
using EnemyRandomizer.App.Services;
using EnemyRandomizer.Core.Configuration;
using Microsoft.Extensions.Options;

namespace EnemyRandomizer.App.ViewModels;

/// <summary>About dialog: fork notice and thanks to the original author.</summary>
public sealed partial class AboutViewModel(IOptions<AppSettings> app, IOptions<MessageSettings> messages, IAppShell shell)
{
    public string Title => messages.Value.Get(MessageKeys.AboutTitle);

    public string Text => messages.Value.Get(MessageKeys.AboutText);

    public string ProjectUrl => app.Value.ProjectUrl;

    public string Thanks => messages.Value.Get(MessageKeys.AboutThanks, app.Value.OriginalAuthor);

    [RelayCommand]
    private void OpenProject() => shell.OpenPath(ProjectUrl);
}
