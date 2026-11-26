using System.IO;
using EnemyRandomizer.App.Services;

namespace EnemyRandomizer.App.Tests;

public sealed class JsonUserPreferencesTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "er-prefs-" + Guid.NewGuid().ToString("N"));

    private string File_ => Path.Combine(_directory, "preferences.json");

    public void Dispose()
    {
        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, recursive: true);
        }
    }

    [Fact]
    public void Missing_file_gives_defaults()
    {
        var data = new JsonUserPreferences(File_).Current;

        Assert.Equal(AppTheme.System, data.Theme);
        Assert.Equal("", data.GameDirectory);
        Assert.Null(data.Width);
    }

    [Fact]
    public void Saved_values_survive_a_restart()
    {
        new JsonUserPreferences(File_).Save(new UserPreferencesData
        {
            Left = 10, Top = 20, Width = 900, Height = 600, Maximized = true, Theme = AppTheme.Dark, GameDirectory = "D:/DS",
        });

        var loaded = new JsonUserPreferences(File_).Current;

        Assert.Equal(900, loaded.Width);
        Assert.True(loaded.Maximized);
        Assert.Equal(AppTheme.Dark, loaded.Theme);
        Assert.Equal("D:/DS", loaded.GameDirectory);
    }

    [Theory]
    [InlineData("{ broken")]
    [InlineData("")]
    [InlineData("null")]
    public void Damaged_file_gives_defaults(string content)
    {
        Directory.CreateDirectory(_directory);
        File.WriteAllText(File_, content);

        Assert.Equal(AppTheme.System, new JsonUserPreferences(File_).Current.Theme);
    }
}
