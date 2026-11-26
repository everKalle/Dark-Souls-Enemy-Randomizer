using EnemyRandomizer.Core.Abstractions;
using EnemyRandomizer.Core.Configuration;
using EnemyRandomizer.Core.Randomization;
using EnemyRandomizer.Core.Settings;
using EnemyRandomizer.Infrastructure.Setup;
using Microsoft.Extensions.Options;

namespace EnemyRandomizer.Infrastructure.Randomization;

/// <summary>
/// The randomizer operations: randomize, copy placement from a layout reference, and revert.
/// The work runs on the thread pool, expected file and data problems become a failed <see cref="OperationResult"/>
/// and a cancel is reported as <see cref="OperationCanceledException"/>.
/// </summary>
public sealed class RandomizerService(
    IGameInstallation installation,
    GameRestorer restorer,
    RandomizationRun run,
    IOptions<MessageSettings> messages) : IRandomizerService
{
    private const string RevertTitle = "Revert to normal";
    private const string CopyTitle = "Enemy copy complete";
    private const string CopyFailedTitle = "Failed to copy enemy placement";
    private const string RandomizeTitle = "Randomization complete";
    private const string RandomizeFailedTitle = "Randomization failed";
    private const string RevertFailedTitle = "Revert failed";

    public Task<OperationResult> RandomizeAsync(RandomizerSettings settings, IProgress<OperationProgress> progress, CancellationToken cancellationToken)
    {
        if (installation.Edition is not { } edition)
        {
            return Task.FromResult(OperationResult.Failed(RandomizeTitle, messages.Value.Get(MessageKeys.ExeNotFound)));
        }

        return Task.Run(
            () =>
            {
                try
                {
                    var result = run.Run(settings, edition, progress, cancellationToken);
                    return OperationResult.Ok(
                        RandomizeTitle,
                        messages.Value.Get(MessageKeys.RandomizeDone, result.Replaced, result.Seed, Path.GetFileName(result.LayoutFile)));
                }
                catch (Exception e) when (IsExpected(e))
                {
                    return OperationResult.Failed(RandomizeFailedTitle, messages.Value.Get(MessageKeys.RandomizeFailed, e.Message));
                }
            },
            cancellationToken);
    }

    public Task<OperationResult> CopyPlacementAsync(string referenceFile, IProgress<OperationProgress> progress, CancellationToken cancellationToken)
    {
        if (installation.Edition is not { } edition)
        {
            return Task.FromResult(OperationResult.Failed(CopyTitle, messages.Value.Get(MessageKeys.ExeNotFound)));
        }

        return Task.Run(
            () =>
            {
                try
                {
                    var source = LayoutReference.Parse(File.ReadLines(referenceFile));
                    run.CopyPlacement(source, edition, progress, cancellationToken);
                    return OperationResult.Ok(CopyTitle, messages.Value.Get(MessageKeys.CopyDone));
                }
                catch (Exception e) when (IsExpected(e))
                {
                    return OperationResult.Failed(CopyFailedTitle, messages.Value.Get(MessageKeys.CopyFailed, e.Message));
                }
            },
            cancellationToken);
    }

    public Task<OperationResult> RevertAsync(bool restoreEffects, IProgress<OperationProgress> progress, CancellationToken cancellationToken)
    {
        if (installation.Edition is not { } edition)
        {
            return Task.FromResult(OperationResult.Failed(RevertTitle, messages.Value.Get(MessageKeys.ExeNotFound)));
        }

        return Task.Run(
            () =>
            {
                try
                {
                    var result = restorer.Revert(edition, restoreEffects, progress, cancellationToken);
                    var text = messages.Value.Get(MessageKeys.RevertDone, result.FilesRestored);
                    if (!result.ParamRestored)
                    {
                        text += "\n\n" + messages.Value.Get(MessageKeys.RevertParamMissing);
                    }

                    return OperationResult.Ok(RevertTitle, text);
                }
                catch (Exception e) when (IsExpected(e))
                {
                    return OperationResult.Failed(RevertFailedTitle, messages.Value.Get(MessageKeys.RevertFailed, e.Message));
                }
            },
            cancellationToken);
    }

    /// <summary>Problems caused by the files or data (locked, read-only, malformed); anything else is a bug and must surface.</summary>
    private static bool IsExpected(Exception e) =>
        e is IOException or UnauthorizedAccessException or InvalidDataException or KeyNotFoundException or InvalidOperationException;
}
