using EnemyRandomizer.Core.Settings;

namespace EnemyRandomizer.Core.Abstractions;

/// <summary>Progress of a long running operation (randomize, copy placement, revert).</summary>
public sealed record OperationProgress(int Step, int TotalSteps, string Message);

/// <summary>Outcome of a long running operation, shown to the user.</summary>
public sealed record OperationResult(bool Success, string Title, string Message)
{
    public static OperationResult Ok(string title, string message) => new(true, title, message);

    public static OperationResult Failed(string title, string message) => new(false, title, message);
}

/// <summary>Writes randomized or restored game files. Implementations never touch the UI.</summary>
public interface IRandomizerService
{
    Task<OperationResult> RandomizeAsync(
        RandomizerSettings settings,
        IProgress<OperationProgress> progress,
        CancellationToken cancellationToken);

    /// <summary>Replays a saved enemy layout (<c>refs/enemy-layout-*.txt</c>) without randomness.</summary>
    Task<OperationResult> CopyPlacementAsync(
        string referenceFile,
        IProgress<OperationProgress> progress,
        CancellationToken cancellationToken);

    /// <summary>Restores the original game files; effect files and the exe only when <paramref name="restoreEffects"/>.</summary>
    Task<OperationResult> RevertAsync(
        bool restoreEffects,
        IProgress<OperationProgress> progress,
        CancellationToken cancellationToken);
}
