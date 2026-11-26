using EnemyRandomizer.Core.Abstractions;
using EnemyRandomizer.Core.Configuration;
using EnemyRandomizer.Core.Randomization;
using EnemyRandomizer.Core.Settings;
using EnemyRandomizer.Infrastructure.Setup;
using Microsoft.Extensions.Options;

namespace EnemyRandomizer.Infrastructure.Randomization;

/// <summary>
/// The randomizer operations: randomize, copy placement from a layout reference, and revert.
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

    public Task<OperationResult> RandomizeAsync(RandomizerSettings settings, IProgress<OperationProgress> progress, CancellationToken cancellationToken)
    {
        if (installation.Edition is not { } edition)
        {
            return Task.FromResult(OperationResult.Failed(RandomizeTitle, messages.Value.Get(MessageKeys.ExeNotFound)));
        }

        var result = run.Run(settings, edition, progress, cancellationToken);
        return Task.FromResult(OperationResult.Ok(
            RandomizeTitle,
            messages.Value.Get(MessageKeys.RandomizeDone, result.Replaced, result.Seed, Path.GetFileName(result.LayoutFile))));
    }

    public Task<OperationResult> CopyPlacementAsync(string referenceFile, IProgress<OperationProgress> progress, CancellationToken cancellationToken)
    {
        if (installation.Edition is not { } edition)
        {
            return Task.FromResult(OperationResult.Failed(CopyTitle, messages.Value.Get(MessageKeys.ExeNotFound)));
        }

        LayoutReference source;
        try
        {
            source = LayoutReference.Parse(File.ReadLines(referenceFile));
            run.CopyPlacement(source, edition, progress, cancellationToken);
        }
        catch (InvalidDataException e)
        {
            return Task.FromResult(OperationResult.Failed(CopyFailedTitle, messages.Value.Get(MessageKeys.CopyFailed, e.Message)));
        }

        return Task.FromResult(OperationResult.Ok(CopyTitle, messages.Value.Get(MessageKeys.CopyDone)));
    }

    public Task<OperationResult> RevertAsync(bool restoreEffects, IProgress<OperationProgress> progress, CancellationToken cancellationToken)
    {
        if (installation.Edition is not { } edition)
        {
            return Task.FromResult(OperationResult.Failed(RevertTitle, messages.Value.Get(MessageKeys.ExeNotFound)));
        }

        var result = restorer.Revert(edition, restoreEffects, progress, cancellationToken);
        var text = messages.Value.Get(MessageKeys.RevertDone, result.FilesRestored);
        if (!result.ParamRestored)
        {
            text += "\n\n" + messages.Value.Get(MessageKeys.RevertParamMissing);
        }

        return Task.FromResult(OperationResult.Ok(RevertTitle, text));
    }
}
