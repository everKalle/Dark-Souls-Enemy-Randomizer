using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EnemyRandomizer.Core.Abstractions;

namespace EnemyRandomizer.App.ViewModels;

/// <summary>State of the in-window progress overlay. Pass a source to offer a Cancel button.</summary>
public sealed partial class ProgressViewModel(string title, CancellationTokenSource? cancellation = null) : ObservableObject
{
    public string Title { get; } = title;

    public bool CanCancel => cancellation is not null;

    [ObservableProperty]
    public partial int Step { get; set; }

    [ObservableProperty]
    public partial int TotalSteps { get; set; } = 1;

    /// <summary>True until the first report arrives, so the bar does not claim a progress it does not know.</summary>
    [ObservableProperty]
    public partial bool IsIndeterminate { get; set; } = true;

    [ObservableProperty]
    public partial string Message { get; set; } = "Preparing...";

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(CancelCommand))]
    public partial bool IsCancelling { get; set; }

    public void Report(OperationProgress progress)
    {
        ArgumentNullException.ThrowIfNull(progress);
        IsIndeterminate = false;
        TotalSteps = Math.Max(1, progress.TotalSteps);
        Step = progress.Step;
        Message = IsCancelling ? "Cancelling after the current map..." : progress.Message;
    }

    [RelayCommand(CanExecute = nameof(CanRequestCancel))]
    private void Cancel()
    {
        IsCancelling = true;
        Message = "Cancelling after the current map...";
        cancellation?.Cancel();
    }

    private bool CanRequestCancel() => CanCancel && !IsCancelling;
}
