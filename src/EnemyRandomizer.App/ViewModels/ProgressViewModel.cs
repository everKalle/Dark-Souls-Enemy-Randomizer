using CommunityToolkit.Mvvm.ComponentModel;
using EnemyRandomizer.Core.Abstractions;

namespace EnemyRandomizer.App.ViewModels;

/// <summary>State of the progress window.</summary>
public sealed partial class ProgressViewModel(string title) : ObservableObject
{
    public string Title { get; } = title;

    [ObservableProperty]
    public partial int Step { get; set; }

    [ObservableProperty]
    public partial int TotalSteps { get; set; } = 1;

    [ObservableProperty]
    public partial string Message { get; set; } = "Preparing...";

    public void Report(OperationProgress progress)
    {
        ArgumentNullException.ThrowIfNull(progress);
        TotalSteps = Math.Max(1, progress.TotalSteps);
        Step = progress.Step;
        Message = progress.Message;
    }
}
