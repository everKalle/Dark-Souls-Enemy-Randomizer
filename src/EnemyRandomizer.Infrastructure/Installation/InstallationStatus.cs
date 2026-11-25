using EnemyRandomizer.Core;
using EnemyRandomizer.Core.Abstractions;
using EnemyRandomizer.Core.Configuration;
using EnemyRandomizer.Core.Installation;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EnemyRandomizer.Infrastructure.Installation;

/// <summary>
/// Startup sequence (legacy <c>randomizer_rng.py:223-280</c>, <c>randomizer.py:625-648</c>): run all checks; when
/// everything is fine patch the PTDE exe, then run the first-time setup and retry it while copies are broken.
/// </summary>
public sealed partial class InstallationStatus(
    IGameInstallation installation,
    CompositeInstallationCheck checks,
    IExePatcher patcher,
    IFirstTimeSetup setup,
    IGamePaths paths,
    IOptions<AppSettings> app,
    ILogger<InstallationStatus> logger) : IInstallationStatus
{
    public InstallationReport? Report { get; private set; }

    public Task<InstallationReport> CheckAsync(IProgress<OperationProgress> progress, CancellationToken cancellationToken) =>
        Task.Run(() => Report = Check(progress, cancellationToken), cancellationToken);

    private InstallationReport Check(IProgress<OperationProgress> progress, CancellationToken cancellationToken)
    {
        installation.Refresh();
        progress.Report(new OperationProgress(0, 1, "Checking game files"));
        var problems = checks.Check(installation.Edition, installation.ExeState).ToList();

        if (problems.Count == 0 && ExeStates.NeedsPatch(installation.ExeState) && !patcher.PatchIfNeeded())
        {
            problems.Add(new InstallationProblem(MessageKeys.ExePatchPermissionDenied));
        }

        var warnings = new List<InstallationProblem>();
        var copiesFailed = false;
        if (problems.Count == 0 && installation.Edition is { } edition)
        {
            var result = RunSetupWithRetries(edition, progress, cancellationToken);
            if (result.EffectsFailed)
            {
                warnings.Add(new InstallationProblem(MessageKeys.EffectsWriteFailedText, edition == GameEdition.Remastered ? ".dcx" : ""));
            }

            copiesFailed = !result.CopiesValid;
            problems.AddRange(CopyProblems(result));
        }

        var report = new InstallationReport(installation.Edition, installation.ExeState, problems)
        {
            Warnings = warnings,
            CopiesFailed = copiesFailed,
        };
        var editionText = report.Edition?.ToString() ?? "none";
        LogReport(logger, editionText, report.ExeState, problems.Count);
        return report;
    }

    /// <summary>Legacy <c>retryFileCopy</c>: run the setup again while copies are missing or broken.</summary>
    private SetupResult RunSetupWithRetries(GameEdition edition, IProgress<OperationProgress> progress, CancellationToken cancellationToken)
    {
        var attempts = Math.Max(1, app.Value.FileCopyRetries);
        SetupResult result;
        do
        {
            result = setup.Run(edition, progress, cancellationToken);
        }
        while (!result.CopiesValid && --attempts > 0);

        return result;
    }

    private IEnumerable<InstallationProblem> CopyProblems(SetupResult result)
    {
        var mapFolder = Path.GetRelativePath(paths.GameDirectory, paths.MapStudioCopyDirectory).Replace('\\', '/');
        var aiFolder = Path.GetRelativePath(paths.GameDirectory, paths.MapAiCopyDirectory).Replace('\\', '/');
        if (result.MissingMapCopies > 0)
        {
            yield return new InstallationProblem(MessageKeys.CopiesMissing, result.MissingMapCopies, result.Maps, ".msb", mapFolder);
        }

        if (result.InvalidMapCopies > 0)
        {
            yield return new InstallationProblem(MessageKeys.CopiesInvalid, result.InvalidMapCopies, result.Maps, ".msb", mapFolder);
        }

        if (result.MissingAiCopies > 0)
        {
            yield return new InstallationProblem(MessageKeys.CopiesMissing, result.MissingAiCopies, result.AiArchives, ".luabnd", aiFolder);
        }

        if (result.InvalidAiCopies > 0)
        {
            yield return new InstallationProblem(MessageKeys.CopiesInvalid, result.InvalidAiCopies, result.AiArchives, ".luabnd", aiFolder);
        }
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Installation: edition {Edition}, exe {ExeState}, {ProblemCount} problem(s)")]
    private static partial void LogReport(ILogger logger, string edition, ExeState exeState, int problemCount);
}
