namespace EnemyRandomizer.Core.Installation;

/// <summary>
/// A problem found at startup. <see cref="MessageKey"/> refers to <c>messages.json</c>,
/// <see cref="Arguments"/> fill its placeholders (Core stays free of UI text).
/// </summary>
public sealed record InstallationProblem(string MessageKey, params object[] Arguments);

/// <summary>Result of the startup checks and first-time setup (legacy <c>Randomizer.__init__</c>, <c>randomizer_rng.py:183-280</c>).</summary>
public sealed record InstallationReport(GameEdition? Edition, ExeState ExeState, IReadOnlyList<InstallationProblem> Problems)
{
    /// <summary>Non-blocking problems shown once as a warning (e.g. effect files could not be written).</summary>
    public IReadOnlyList<InstallationProblem> Warnings { get; init; } = [];

    /// <summary>The data copies made by the first-time setup are missing or broken (user may retry).</summary>
    public bool CopiesFailed { get; init; }

    /// <summary>True when no check failed: randomize and revert may run.</summary>
    public bool CanRandomize => Problems.Count == 0;

    /// <summary>The PTDE exe hash is unknown: allowed, but the user is warned once.</summary>
    public bool ShowUnknownExeWarning => ExeState == ExeState.Unknown;
}

/// <summary>Outcome of the first-time setup (legacy <c>firstTimeSetup</c> + <c>checkCopiedFiles</c>).</summary>
public sealed record SetupResult(
    int Maps,
    int MissingMapCopies,
    int InvalidMapCopies,
    int AiArchives,
    int MissingAiCopies,
    int InvalidAiCopies,
    bool EffectsFailed)
{
    public bool CopiesValid => MissingMapCopies + InvalidMapCopies + MissingAiCopies + InvalidAiCopies == 0;
}

/// <summary>Exe states with which randomizing is allowed (legacy <c>randomizer_rng.py:257</c>).</summary>
public static class ExeStates
{
    public static bool AllowsRandomizing(ExeState state) => state is
        ExeState.Unpacked or ExeState.UnpackedDebug or ExeState.Unknown
        or ExeState.Patched or ExeState.PatchedDebug or ExeState.Remastered;

    /// <summary>States that still need the memory patch.</summary>
    public static bool NeedsPatch(ExeState state) => state is ExeState.Unpacked or ExeState.UnpackedDebug;

    public static bool IsDebug(ExeState state) => state is
        ExeState.UnpackedDebug or ExeState.NotUnpackedDebug or ExeState.PatchedDebug;

    public static GameEdition? EditionOf(ExeState state) => state switch
    {
        ExeState.NotFound => null,
        ExeState.Remastered => GameEdition.Remastered,
        _ => GameEdition.Ptde,
    };
}
