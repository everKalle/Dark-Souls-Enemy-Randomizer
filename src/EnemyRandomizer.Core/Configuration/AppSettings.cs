namespace EnemyRandomizer.Core.Configuration;

/// <summary>General application settings (<c>config/appsettings.json</c>, section <c>App</c>).</summary>
public sealed class AppSettings : IValidatableSettings
{
    public static string SectionName => "App";

    /// <summary>Window title prefix; the version is taken from the assembly.</summary>
    public string Title { get; set; } = "";

    /// <summary>Repository of this .NET fork (shown in the About dialog).</summary>
    public string ProjectUrl { get; set; } = "";

    /// <summary>Author of the original Python randomizer (credited in the About dialog).</summary>
    public string OriginalAuthor { get; set; } = "";

    /// <summary>Game install folder. Empty means the folder of the exe (the normal install).</summary>
    public string GameDirectory { get; set; } = "";

    /// <summary>Randomizer data folder, relative to the game folder.</summary>
    public string DataFolder { get; set; } = "";

    /// <summary>Sub folders inside <see cref="DataFolder"/>.</summary>
    public DataFolderSettings DataFolders { get; set; } = new();

    /// <summary>Files inside <see cref="DataFolder"/> that must exist (legacy <c>checkProperUnpack</c>).</summary>
    public List<string> RequiredDataFiles { get; set; } = [];

    /// <summary>How often a failed first-time file copy is retried.</summary>
    public int FileCopyRetries { get; set; }

    public IEnumerable<string> Validate()
    {
        var errors = ValidationHelpers.RequireText(Title, "App.Title")
            .Concat(ValidationHelpers.RequireText(OriginalAuthor, "App.OriginalAuthor"))
            .Concat(ValidationHelpers.RequireAbsoluteUrl(ProjectUrl, "App.ProjectUrl"))
            .Concat(ValidationHelpers.RequireText(DataFolder, "App.DataFolder"))
            .Concat(ValidationHelpers.RequireRange(FileCopyRetries, 1, 50, "App.FileCopyRetries"))
            .Concat(DataFolders.Validate());

        foreach (var error in errors)
        {
            yield return error;
        }
    }
}

/// <summary>Folder names inside the randomizer data folder.</summary>
public sealed class DataFolderSettings
{
    public string MapStudioCopies { get; set; } = "";
    public string MapAiCopies { get; set; } = "";
    public string SfxCopies { get; set; } = "";
    public string AiScripts { get; set; } = "";
    public string ParamBackup { get; set; } = "";
    public string Logs { get; set; } = "";
    public string LayoutReferences { get; set; } = "";
    public string CustomConfigs { get; set; } = "";
    public string Emevd { get; set; } = "";
    public string ReplacementReferences { get; set; } = "";
    public string OriginalEnemyReferences { get; set; } = "";

    internal IEnumerable<string> Validate()
    {
        var folders = new Dictionary<string, string>
        {
            [nameof(MapStudioCopies)] = MapStudioCopies,
            [nameof(MapAiCopies)] = MapAiCopies,
            [nameof(SfxCopies)] = SfxCopies,
            [nameof(AiScripts)] = AiScripts,
            [nameof(ParamBackup)] = ParamBackup,
            [nameof(Logs)] = Logs,
            [nameof(LayoutReferences)] = LayoutReferences,
            [nameof(CustomConfigs)] = CustomConfigs,
            [nameof(Emevd)] = Emevd,
            [nameof(ReplacementReferences)] = ReplacementReferences,
            [nameof(OriginalEnemyReferences)] = OriginalEnemyReferences,
        };

        return folders.SelectMany(f => ValidationHelpers.RequireText(f.Value, $"App.DataFolders.{f.Key}"));
    }
}
