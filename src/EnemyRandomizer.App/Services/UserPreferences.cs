using EnemyRandomizer.Infrastructure.Game;
using System.IO;
using System.Text.Json;

namespace EnemyRandomizer.App.Services;

/// <summary>Per-user UI state that must be readable before the game folder is known (window bounds, theme, folder).</summary>
public sealed record UserPreferencesData
{
    public double? Left { get; init; }

    public double? Top { get; init; }

    public double? Width { get; init; }

    public double? Height { get; init; }

    public bool Maximized { get; init; }

    public AppTheme Theme { get; init; } = AppTheme.System;

    /// <summary>Game folder chosen in the UI; empty means "the folder of the exe".</summary>
    public string GameDirectory { get; init; } = "";
}

public interface IUserPreferences
{
    UserPreferencesData Current { get; }

    void Save(UserPreferencesData data);
}

/// <summary>Stores <see cref="UserPreferencesData"/> as JSON. A missing or corrupt file yields the defaults.</summary>
public sealed class JsonUserPreferences : IUserPreferences
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    private readonly string _file;

    public JsonUserPreferences(string file)
    {
        _file = file;
        Current = Read(file);
    }

    /// <summary><c>%LocalAppData%\EnemyRandomizer\preferences.json</c>.</summary>
    public static string DefaultFile { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "EnemyRandomizer", "preferences.json");

    public UserPreferencesData Current { get; private set; }

    public void Save(UserPreferencesData data)
    {
        ArgumentNullException.ThrowIfNull(data);
        Current = data;
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_file)!);
            AtomicFile.WriteAllText(_file, JsonSerializer.Serialize(data, JsonOptions));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // Preferences are a convenience; failing to store them must never break the app.
        }
    }

    private static UserPreferencesData Read(string file)
    {
        try
        {
            return File.Exists(file)
                ? JsonSerializer.Deserialize<UserPreferencesData>(File.ReadAllText(file), JsonOptions) ?? new UserPreferencesData()
                : new UserPreferencesData();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
        {
            return new UserPreferencesData();
        }
    }
}
