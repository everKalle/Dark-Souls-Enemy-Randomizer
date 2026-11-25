using EnemyRandomizer.Core.Settings;

namespace EnemyRandomizer.Core.Tests.Settings;

public class LegacyConfigStringTests
{
    [Fact]
    public void Format_orders_by_legacy_index_and_quotes_seed()
    {
        var options = TestOptions.Create();
        var settings = RandomizerSettings.CreateDefault(options) with { Seed = "abc" };

        Assert.Equal("1/-/50/-/'''abc'''", LegacyConfigString.Format(settings, options));
    }

    [Fact]
    public void Parse_round_trips()
    {
        var options = TestOptions.Create();

        Assert.True(LegacyConfigString.TryParse("0/-/75/-/'''my seed'''", options, "Custom", out var settings, out _));
        Assert.Equal(0, settings!["BossMode"]);
        Assert.Equal(75, settings["Chance"]);
        Assert.Equal("my seed", settings.Seed);
        Assert.Equal("Custom", settings.EnemyConfig);
        Assert.Equal("0/-/75/-/'''my seed'''", LegacyConfigString.Format(settings, options));
    }

    [Theory]
    [InlineData("1/-/50")]                      // too few fields
    [InlineData("1/-/50/-/x/-/''''''")]         // too many fields
    [InlineData("7/-/50/-/''''''")]             // not an allowed choice
    [InlineData("1/-/101/-/''''''")]            // slider out of range
    [InlineData("a/-/50/-/''''''")]             // not a number
    public void Parse_rejects_invalid_text(string text)
    {
        Assert.False(LegacyConfigString.TryParse(text, TestOptions.Create(), "Default", out var settings, out var error));
        Assert.Null(settings);
        Assert.NotEmpty(error);
    }
}

public class RandomizerSettingsTests
{
    [Fact]
    public void Normalize_drops_unknown_and_fixes_invalid_values()
    {
        var options = TestOptions.Create();
        var settings = new RandomizerSettings(
            new Dictionary<string, int> { ["BossMode"] = 9, ["Removed"] = 3 }, "s", "");

        var normalized = settings.Normalize(options);

        Assert.Equal(1, normalized["BossMode"]);   // invalid -> default
        Assert.Equal(50, normalized["Chance"]);    // missing -> default
        Assert.False(normalized.Values.ContainsKey("Removed"));
        Assert.Equal(RandomizerSettings.DefaultEnemyConfig, normalized.EnemyConfig);
    }

    [Fact]
    public void Keys_are_case_insensitive()
    {
        var settings = RandomizerSettings.CreateDefault(TestOptions.Create());
        Assert.Equal(1, settings["bossmode"]);
    }
}
