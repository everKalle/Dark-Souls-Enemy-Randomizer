using EnemyRandomizer.Core;
using EnemyRandomizer.Core.Configuration;
using EnemyRandomizer.Infrastructure.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace EnemyRandomizer.Infrastructure.Tests.Configuration;

/// <summary>Loads the JSON files shipped with the app and checks them against the legacy Python values.</summary>
public sealed class ShippedConfigurationTests : IDisposable
{
    private readonly ServiceProvider _services;

    public ShippedConfigurationTests()
    {
        var configuration = new ConfigurationBuilder()
            .AddRandomizerConfigFiles(AppContext.BaseDirectory)
            .Build();

        _services = new ServiceCollection()
            .AddRandomizerSettings(configuration)
            .BuildServiceProvider();
    }

    public void Dispose() => _services.Dispose();

    private T Get<T>() where T : class => _services.GetRequiredService<IOptions<T>>().Value;

    [Fact]
    public void All_config_files_are_present()
    {
        foreach (var file in ConfigurationFiles.FileNames)
        {
            Assert.True(File.Exists(Path.Combine(AppContext.BaseDirectory, ConfigurationFiles.FolderName, file)), file);
        }
    }

    [Fact]
    public void All_settings_bind_and_validate()
    {
        // Accessing .Value runs every registered IValidateOptions and throws on errors.
        var exception = Record.Exception(() =>
        {
            Get<AppSettings>();
            Get<GameDataSettings>();
            Get<RandomizerOptionSettings>();
            Get<DifficultyCurveSettings>();
            Get<ReplacementRuleSettings>();
            Get<MapPatchSettings>();
            Get<MsbModelSettings>();
            Get<MessageSettings>();
        });

        Assert.Null(exception);
    }

    [Fact]
    public void Maps_match_legacy_tables()
    {
        var maps = Get<GameDataSettings>().Maps;

        Assert.Equal(18, maps.Count);                              // inputFilesAll
        Assert.Equal(17, maps.Count(m => m.Randomized));           // inputFiles
        Assert.Equal(
            [250, 495, 171, 259, 280, 459, 282, 254, 242, 180, 342, 382, 210, 251, 197, 307, 84, 154],
            maps.Select(m => m.StartIndices[GameEdition.Ptde]));
        Assert.Equal(
            [250, 495, 171, 259, 280, 464, 282, 254, 242, 181, 342, 382, 210, 251, 197, 307, 84, 154],
            maps.Select(m => m.StartIndices[GameEdition.Remastered]));
    }

    [Fact]
    public void Ptde_exe_hashes_cover_all_known_states()
    {
        var ptde = Get<GameDataSettings>().Editions[GameEdition.Ptde];

        Assert.Equal(6, ptde.ExeHashes.Count);
        Assert.Equal(ExeState.Patched, ptde.ExeHashes["5aaea26acfd72976812f00b0bf6dbdc82d78998e161d4e2c17d4a05684f24aad"]);
        // check_exe.py:73-74: b"h\x00\x00\xa2\x00" -> b"h\x00\x00\xda\x00"
        Assert.Equal(new byte[] { 0x68, 0x00, 0x00, 0xA2, 0x00 }, ExePatchSettings.ParseHex(ptde.ExePatch!.Search));
        Assert.Equal(new byte[] { 0x68, 0x00, 0x00, 0xDA, 0x00 }, ExePatchSettings.ParseHex(ptde.ExePatch.Replace));
    }

    [Fact]
    public void Options_match_legacy_defaults_string()
    {
        // randomizer.py:1501 RestoreDefaultSettings, without the trailing seed field.
        const string legacyDefaults = "1/-/2/-/0/-/1/-/0/-/3/-/100/-/10/-/90/-/1/-/1/-/0/-/50/-/1/-/1/-/1/-/1/-/0/-/0/-/0/-/1";
        var options = Get<RandomizerOptionSettings>().Options;

        var actual = string.Join("/-/", options.OrderBy(o => o.LegacyIndex).Select(o => o.Default));

        Assert.Equal(21, options.Count);
        Assert.Equal(legacyDefaults, actual);
    }

    [Fact]
    public void Tail_models_follow_character_models()
    {
        var models = Get<MsbModelSettings>();
        var tails = Get<MapPatchSettings>().TailCuts;

        Assert.Equal(123, models.CharacterModels.Count);
        Assert.Equal(models.TailModels.Count, tails.Count);
        Assert.Equal(
            Enumerable.Range(models.CharacterModels.Count, tails.Count),
            tails.Select(t => t.RelativeModelIndex));
    }

    [Fact]
    public void Gwyn_rejection_follows_option_value()
    {
        var gwyn = Get<ReplacementRuleSettings>().RandomRejections.Single(r => r.Name == "Gwyn");

        Assert.Equal("GwynNerf", gwyn.Option);
        Assert.Equal(85, gwyn.ChanceByOptionValue[0]);
        Assert.Equal(60, gwyn.ChanceByOptionValue[1]);
    }
}
