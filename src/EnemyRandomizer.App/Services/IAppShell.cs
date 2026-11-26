namespace EnemyRandomizer.App.Services;

/// <summary>Operating-system and application-level actions view models need (kept out of the view models for testing).</summary>
public interface IAppShell
{
    /// <summary>Returns the chosen folder or <c>null</c> when cancelled.</summary>
    string? PickFolder(string? initialDirectory);

    /// <summary>Opens a folder, file or https address with the default application.</summary>
    void OpenPath(string path);

    void CopyText(string text);

    void ApplyTheme(AppTheme theme);

    /// <summary>Starts a new instance of the app and closes this one.</summary>
    void Restart();
}
