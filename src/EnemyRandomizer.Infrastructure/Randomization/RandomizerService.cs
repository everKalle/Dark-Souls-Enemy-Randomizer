using EnemyRandomizer.Core.Abstractions;
using EnemyRandomizer.Core.Configuration;
using EnemyRandomizer.Core.Settings;
using EnemyRandomizer.Infrastructure.Setup;
using Microsoft.Extensions.Options;

namespace EnemyRandomizer.Infrastructure.Randomization;

/// <summary>
/// The randomizer operations. Randomize and revert are real; copy placement still delegates to the preview until
/// step 8.
/// </summary>
public sealed class RandomizerService(
    IGameInstallation installation,
    GameRestorer restorer,
    RandomizationRun run,
    PreviewRandomizerService preview,
    IOptions<MessageSettings> messages) : IRandomizerService
{
    private const string RevertTitle = "Revert to normal";
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

    public Task<OperationResult> CopyPlacementAsync(string referenceFile, IProgress<OperationProgress> progress, CancellationToken cancellationToken) =>
        preview.CopyPlacementAsync(referenceFile, progress, cancellationToken);

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
