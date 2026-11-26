using System.Text.Json;
using EnemyRandomizer.Core.Abstractions;
using EnemyRandomizer.Core.Configuration;
using EnemyRandomizer.Core.Settings;
using EnemyRandomizer.Infrastructure.Game;
using Microsoft.Extensions.Options;

namespace EnemyRandomizer.Infrastructure.Settings;

/// <summary>
/// Stores the last used settings as readable JSON. When only the v0.4.x <c>lastConfig.txt</c> exists
/// (line 1: enemy config, line 2: text config) it is imported once.
/// </summary>
public sealed class JsonLastConfigStore(IGamePaths paths, IOptions<RandomizerOptionSettings> options) : ILastConfigStore
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public RandomizerSettings? Load()
    {
        try
        {
            if (File.Exists(paths.LastConfigFile))
            {
                var document = JsonSerializer.Deserialize<LastConfigDocument>(File.ReadAllText(paths.LastConfigFile), JsonOptions);
                return document?.Options is null
                    ? null
                    : new RandomizerSettings(document.Options, document.Seed ?? "", document.EnemyConfig ?? RandomizerSettings.DefaultEnemyConfig);
            }

            return LoadLegacy();
        }
        catch (Exception ex) when (ex is JsonException or IOException or UnauthorizedAccessException)
        {
            // A damaged settings file must not stop the app from starting; fall back to the defaults.
            return null;
        }
    }

    public void Save(RandomizerSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        var document = new LastConfigDocument(
            settings.EnemyConfig,
            settings.Seed,
            settings.Values.ToDictionary(v => v.Key, v => v.Value));

        Directory.CreateDirectory(Path.GetDirectoryName(paths.LastConfigFile)!);
        AtomicFile.WriteAllText(paths.LastConfigFile, JsonSerializer.Serialize(document, JsonOptions));
    }

    private RandomizerSettings? LoadLegacy()
    {
        if (!File.Exists(paths.LegacyLastConfigFile))
        {
            return null;
        }

        var lines = File.ReadAllLines(paths.LegacyLastConfigFile);
        if (lines.Length < 2)
        {
            return null;
        }

        return LegacyConfigString.TryParse(lines[1], options.Value, lines[0].Trim(), out var settings, out _)
            ? settings
            : null;
    }

    private sealed record LastConfigDocument(string EnemyConfig, string Seed, Dictionary<string, int> Options);
}
