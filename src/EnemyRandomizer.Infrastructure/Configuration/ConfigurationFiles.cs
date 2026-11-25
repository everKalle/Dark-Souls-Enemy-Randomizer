using Microsoft.Extensions.Configuration;

namespace EnemyRandomizer.Infrastructure.Configuration;

/// <summary>The JSON files in the <c>config</c> folder next to the exe.</summary>
public static class ConfigurationFiles
{
    public const string FolderName = "config";

    public static IReadOnlyList<string> FileNames { get; } =
    [
        "appsettings.json",
        "game-editions.json",
        "randomizer-options.json",
        "difficulty-curves.json",
        "rules.json",
        "map-patches.json",
        "models.json",
        "messages.json",
    ];

    /// <summary>
    /// Adds every config file from <paramref name="baseDirectory"/>/config. All files are required;
    /// a missing file is a broken install and fails at startup with a clear message.
    /// </summary>
    public static IConfigurationBuilder AddRandomizerConfigFiles(this IConfigurationBuilder builder, string baseDirectory)
    {
        var folder = Path.Combine(baseDirectory, FolderName);
        foreach (var file in FileNames)
        {
            builder.AddJsonFile(Path.Combine(folder, file), optional: false, reloadOnChange: false);
        }

        return builder;
    }
}
