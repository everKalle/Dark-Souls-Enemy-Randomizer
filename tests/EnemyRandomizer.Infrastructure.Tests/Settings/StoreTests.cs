using EnemyRandomizer.Core;
using EnemyRandomizer.Core.Configuration;
using EnemyRandomizer.Core.Settings;
using EnemyRandomizer.Infrastructure.Settings;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace EnemyRandomizer.Infrastructure.Tests.Settings;

public sealed class EnemyConfigRepositoryTests : IDisposable
{
    private readonly TempGameFolder _folder = new();

    public void Dispose() => _folder.Dispose();

    private EnemyConfigRepository Create(GameEdition edition = GameEdition.Ptde) =>
        new(_folder.Paths, new FixedInstallation(edition));

    [Fact]
    public void Default_config_is_the_shipped_catalog()
    {
        var repository = Create();

        Assert.Equal([RandomizerSettings.DefaultEnemyConfig], repository.ListNames());
        var entries = repository.Load(RandomizerSettings.DefaultEnemyConfig);
        Assert.Equal(123, entries.Count);
        Assert.False(entries.Single(e => e.Id == "c4100").Enabled);  // Artorias: Remastered only
    }

    [Fact]
    public void Artorias_is_enabled_on_remastered() =>
        Assert.True(Create(GameEdition.Remastered).Load("Default").Single(e => e.Id == "c4100").Enabled);

    [Fact]
    public void Save_creates_custom_config_and_notifies()
    {
        var repository = Create();
        var notified = false;
        repository.ConfigsChanged += (_, _) => notified = true;
        var flags = repository.Load("Default").Select(_ => false).ToList();

        var file = repository.Save("No enemies", flags);

        Assert.True(notified);
        Assert.True(File.Exists(file));
        Assert.Equal(["Default", "No enemies"], repository.ListNames());
        Assert.All(repository.Load("No enemies"), e => Assert.False(e.Enabled));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" padded ")]
    [InlineData("Default")]
    [InlineData("a/b")]
    [InlineData("bad:name")]
    public void Invalid_names_are_rejected(string name) => Assert.False(Create().IsValidName(name));
}

public sealed class JsonLastConfigStoreTests : IDisposable
{
    private readonly TempGameFolder _folder = new();
    private readonly IOptions<RandomizerOptionSettings> _options;

    public JsonLastConfigStoreTests()
    {
        var configuration = new ConfigurationBuilder()
            .AddJsonFile(Path.Combine(AppContext.BaseDirectory, "config", "randomizer-options.json"))
            .Build();
        _options = Options.Create(configuration.GetSection(RandomizerOptionSettings.SectionName).Get<RandomizerOptionSettings>()!);
    }

    public void Dispose() => _folder.Dispose();

    [Fact]
    public void Nothing_saved_returns_null() =>
        Assert.Null(new JsonLastConfigStore(_folder.Paths, _options).Load());

    [Fact]
    public void Save_and_load_round_trip()
    {
        var store = new JsonLastConfigStore(_folder.Paths, _options);
        var settings = RandomizerSettings.CreateDefault(_options.Value) with { Seed = "seed", EnemyConfig = "Mine" };

        store.Save(settings);
        var loaded = store.Load()!;

        Assert.Equal("seed", loaded.Seed);
        Assert.Equal("Mine", loaded.EnemyConfig);
        Assert.Equal(settings.Values.OrderBy(v => v.Key), loaded.Values.OrderBy(v => v.Key));
    }

    [Fact]
    public void Imports_legacy_last_config_txt()
    {
        Directory.CreateDirectory(_folder.Paths.DataDirectory);
        File.WriteAllLines(_folder.Paths.LegacyLastConfigFile,
        [
            "MyConfig",
            "3/-/3/-/0/-/1/-/0/-/2/-/100/-/10/-/90/-/1/-/1/-/0/-/50/-/1/-/1/-/1/-/1/-/0/-/0/-/0/-/1/-/'''old seed'''",
        ]);

        var loaded = new JsonLastConfigStore(_folder.Paths, _options).Load()!;

        Assert.Equal("MyConfig", loaded.EnemyConfig);
        Assert.Equal("old seed", loaded.Seed);
        Assert.Equal(3, loaded["BossMode"]);
        Assert.Equal(2, loaded["DifficultyMode"]);
    }
}
