using EnemyRandomizer.Core.Abstractions;
using EnemyRandomizer.Core.Configuration;
using EnemyRandomizer.Core.Settings;
using Microsoft.Extensions.Options;

namespace EnemyRandomizer.Infrastructure.Randomization;

/// <summary>
/// Placeholder until the engine is ported (migration steps 5-8): walks through the same progress steps as the
/// real run (1 + 4 per map, legacy <c>randomizer.py:1009</c>) but never touches game files.
/// </summary>
public sealed class PreviewRandomizerService(IOptions<GameDataSettings> gameData) : IRandomizerService
{
    private static readonly TimeSpan StepDelay = TimeSpan.FromMilliseconds(20);
    private static readonly string[] MapSteps = ["Randomizing", "Saving scripts", "Saving events", "Saving map"];

    public Task<OperationResult> RandomizeAsync(
        RandomizerSettings settings,
        IProgress<OperationProgress> progress,
        CancellationToken cancellationToken) =>
        SimulateAsync("Randomize", progress, cancellationToken);

    public Task<OperationResult> CopyPlacementAsync(
        string referenceFile,
        IProgress<OperationProgress> progress,
        CancellationToken cancellationToken) =>
        SimulateAsync("Copy enemy placement", progress, cancellationToken);

    public Task<OperationResult> RevertAsync(
        bool restoreEffects,
        IProgress<OperationProgress> progress,
        CancellationToken cancellationToken) =>
        SimulateAsync("Revert to normal", progress, cancellationToken);

    private async Task<OperationResult> SimulateAsync(
        string operation,
        IProgress<OperationProgress> progress,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(progress);
        var maps = gameData.Value.Maps.Where(m => m.Randomized).ToList();
        var total = 1 + (maps.Count * MapSteps.Length);
        var step = 0;

        progress.Report(new OperationProgress(++step, total, "Loading files"));
        foreach (var map in maps)
        {
            foreach (var action in MapSteps)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await Task.Delay(StepDelay, cancellationToken).ConfigureAwait(false);
                progress.Report(new OperationProgress(++step, total, $"{action}: {map.Name}"));
            }
        }

        return OperationResult.Ok(
            operation,
            "Preview only: the game file engine is not migrated yet, no files were changed.");
    }
}
