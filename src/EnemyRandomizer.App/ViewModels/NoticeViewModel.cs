namespace EnemyRandomizer.App.ViewModels;

public enum NoticeKind
{
    Success,
    Warning,
    Error,
}

/// <summary>A message shown in the window (banner) instead of a modal box, with an optional "open folder" action.</summary>
public sealed record NoticeViewModel(NoticeKind Kind, string Title, string Message, string? FolderPath = null, string? FolderLabel = null)
{
    public bool IsError => Kind == NoticeKind.Error;

    public bool HasFolder => FolderPath is not null;

    public string Details => Title + Environment.NewLine + Message;
}
