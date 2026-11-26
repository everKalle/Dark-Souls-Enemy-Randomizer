namespace EnemyRandomizer.Core.Configuration;

/// <summary>
/// User facing texts (<c>config/messages.json</c>, section <c>Messages</c>). Keys are listed in <see cref="MessageKeys"/>.
/// Texts may contain <see cref="string.Format(string, object?[])"/> placeholders.
/// </summary>
public sealed class MessageSettings : IValidatableSettings
{
    public static string SectionName => "Messages";

    public Dictionary<string, string> Texts { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Returns the formatted text for <paramref name="key"/>, or the key itself when missing.</summary>
    public string Get(string key, params object[] args) =>
        Texts.TryGetValue(key, out var text)
            ? (args.Length == 0 ? text : string.Format(System.Globalization.CultureInfo.InvariantCulture, text, args))
            : key;

    public IEnumerable<string> Validate() =>
        MessageKeys.All.Where(k => !Texts.TryGetValue(k, out var text) || string.IsNullOrWhiteSpace(text))
                       .Select(k => $"Messages.Texts is missing '{k}'.");
}

/// <summary>Known message keys (legacy <c>randomizer.py:628-974</c>).</summary>
public static class MessageKeys
{
    public const string StartupErrorsHeader = nameof(StartupErrorsHeader);
    public const string ExeNotUnpacked = nameof(ExeNotUnpacked);
    public const string ExeDebugNotUnpacked = nameof(ExeDebugNotUnpacked);
    public const string ExeNotFound = nameof(ExeNotFound);
    public const string ExePatchPermissionDenied = nameof(ExePatchPermissionDenied);
    public const string UnknownExeTitle = nameof(UnknownExeTitle);
    public const string UnknownExeText = nameof(UnknownExeText);
    public const string GameFilesMissingPtde = nameof(GameFilesMissingPtde);
    public const string GameFilesMissingRemastered = nameof(GameFilesMissingRemastered);
    public const string NoFilesInFolder = nameof(NoFilesInFolder);
    public const string FilesMissingInFolder = nameof(FilesMissingInFolder);
    public const string DataFolderMissing = nameof(DataFolderMissing);
    public const string DataFileMissing = nameof(DataFileMissing);
    public const string NoOriginalReferences = nameof(NoOriginalReferences);
    public const string OriginalReferencesMissing = nameof(OriginalReferencesMissing);
    public const string NoWritePermission = nameof(NoWritePermission);
    public const string CopiesMissing = nameof(CopiesMissing);
    public const string CopiesInvalid = nameof(CopiesInvalid);
    public const string AboutTitle = nameof(AboutTitle);
    public const string AboutText = nameof(AboutText);
    public const string AboutThanks = nameof(AboutThanks);
    public const string CopyInfoTitle = nameof(CopyInfoTitle);
    public const string CopyInfoText = nameof(CopyInfoText);
    public const string TextConfigHelp = nameof(TextConfigHelp);
    public const string TextConfigInvalid = nameof(TextConfigInvalid);
    public const string RevertAskEffects = nameof(RevertAskEffects);
    public const string UnsavedEnemyConfig = nameof(UnsavedEnemyConfig);
    public const string EnemyConfigExists = nameof(EnemyConfigExists);
    public const string EnemyConfigSaved = nameof(EnemyConfigSaved);
    public const string EnemyConfigSaveFailed = nameof(EnemyConfigSaveFailed);
    public const string EnemyConfigInvalidName = nameof(EnemyConfigInvalidName);
    public const string ReferenceFileFilter = nameof(ReferenceFileFilter);
    public const string SetupProgressTitle = nameof(SetupProgressTitle);
    public const string CopiesFailedTitle = nameof(CopiesFailedTitle);
    public const string CopiesFailedText = nameof(CopiesFailedText);
    public const string EffectsWriteFailedTitle = nameof(EffectsWriteFailedTitle);
    public const string EffectsWriteFailedText = nameof(EffectsWriteFailedText);
    public const string RevertDone = nameof(RevertDone);
    public const string RevertParamMissing = nameof(RevertParamMissing);
    public const string SetupFailed = nameof(SetupFailed);
    public const string RandomizeDone = nameof(RandomizeDone);
    public const string CopyDone = nameof(CopyDone);
    public const string CopyFailed = nameof(CopyFailed);
    public const string RandomizingTitle = nameof(RandomizingTitle);
    public const string RevertingTitle = nameof(RevertingTitle);
    public const string CopyingTitle = nameof(CopyingTitle);
    public const string RevertTitle = nameof(RevertTitle);
    public const string CancelledTitle = nameof(CancelledTitle);
    public const string CancelledText = nameof(CancelledText);
    public const string ChangeFolderTitle = nameof(ChangeFolderTitle);
    public const string ChangeFolderText = nameof(ChangeFolderText);
    public const string TextConfigApplied = nameof(TextConfigApplied);
    public const string TextConfigCopied = nameof(TextConfigCopied);
    public const string EnemyConfigReplaceHint = nameof(EnemyConfigReplaceHint);

    public static IReadOnlyList<string> All { get; } =
    [
        StartupErrorsHeader, ExeNotUnpacked, ExeDebugNotUnpacked, ExeNotFound, ExePatchPermissionDenied,
        UnknownExeTitle, UnknownExeText, GameFilesMissingPtde, GameFilesMissingRemastered, NoFilesInFolder,
        FilesMissingInFolder, DataFolderMissing, DataFileMissing, NoOriginalReferences, OriginalReferencesMissing,
        NoWritePermission, CopiesMissing, CopiesInvalid, AboutTitle, AboutText, AboutThanks, CopyInfoTitle,
        CopyInfoText, TextConfigHelp, TextConfigInvalid, RevertAskEffects, UnsavedEnemyConfig, EnemyConfigExists,
        EnemyConfigSaved, EnemyConfigSaveFailed, EnemyConfigInvalidName, ReferenceFileFilter, SetupProgressTitle,
        CopiesFailedTitle, CopiesFailedText, EffectsWriteFailedTitle, EffectsWriteFailedText, RevertDone, RevertParamMissing,
        SetupFailed, RandomizeDone, CopyDone, CopyFailed,
        RandomizingTitle, RevertingTitle, CopyingTitle, RevertTitle, CancelledTitle, CancelledText, ChangeFolderTitle, ChangeFolderText, TextConfigApplied, TextConfigCopied, EnemyConfigReplaceHint,
    ];
}
