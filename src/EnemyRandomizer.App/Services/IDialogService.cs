using EnemyRandomizer.Core.Abstractions;

namespace EnemyRandomizer.App.Services;

/// <summary>
/// Everything a view model needs from windows and message boxes. Keeps view models free of WPF types,
/// so they can be unit tested with a fake.
/// </summary>
public interface IDialogService
{
    void ShowInfo(string title, string message);

    void ShowWarning(string title, string message);

    void ShowError(string title, string message);

    bool Confirm(string title, string message);

    /// <summary>Returns the chosen file or <c>null</c> when cancelled.</summary>
    string? OpenFile(string filter, string? initialDirectory);

    void ShowTextConfig();

    void ShowEnemyConfigEditor();

    void ShowAbout();

    /// <summary>Runs <paramref name="work"/> off the UI thread while a progress window is shown.</summary>
    Task<T> RunWithProgressAsync<T>(
        string title,
        Func<IProgress<OperationProgress>, CancellationToken, Task<T>> work);
}

/// <summary>Posts work to the UI thread (used to undo a selection after the binding finished).</summary>
public interface IUiThread
{
    void Post(Action action);
}
