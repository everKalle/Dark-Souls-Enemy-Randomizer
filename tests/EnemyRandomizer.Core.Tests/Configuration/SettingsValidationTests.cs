using EnemyRandomizer.Core;
using EnemyRandomizer.Core.Configuration;

namespace EnemyRandomizer.Core.Tests.Configuration;

public class DifficultyCurveSettingsTests
{
    private static DifficultyCurveSettings Valid() => new()
    {
        MinClass = 0,
        MaxClass = 7,
        Offsets = [1, 0],
        Curves = [new StrictnessCurve { Name = "Strict", Chances = [20, 80] }],
    };

    [Fact]
    public void Valid_curve_has_no_errors() => Assert.Empty(Valid().Validate());

    [Fact]
    public void Chances_must_add_up_to_100()
    {
        var settings = Valid();
        settings.Curves[0].Chances = [20, 70];
        Assert.Contains(settings.Validate(), e => e.Contains("add up to 90", StringComparison.Ordinal));
    }

    [Fact]
    public void Chances_and_offsets_must_have_same_length()
    {
        var settings = Valid();
        settings.Curves[0].Chances = [100];
        Assert.Contains(settings.Validate(), e => e.Contains("offsets", StringComparison.Ordinal));
    }
}

public class RandomizerOptionSettingsTests
{
    private static RandomizerOptionSettings Valid() => new()
    {
        Tabs = [new OptionTab { Id = "main", Title = "Main" }],
        Options =
        [
            new OptionDefinition
            {
                Key = "BossMode", LegacyIndex = 0, Tab = "main", Kind = OptionKind.Choice, Title = "Bosses", Default = 1,
                Choices = [new OptionChoice { Value = 0, Label = "No" }, new OptionChoice { Value = 1, Label = "Yes" }],
            },
            new OptionDefinition
            {
                Key = "Chance", LegacyIndex = 1, Tab = "main", Kind = OptionKind.Slider, Title = "Chance", Default = 50, Min = 0, Max = 100,
                EnabledWhen = [new OptionCondition { Option = "BossMode", Values = [1] }],
            },
        ],
    };

    [Fact]
    public void Valid_options_have_no_errors() => Assert.Empty(Valid().Validate());

    [Fact]
    public void Default_must_be_an_allowed_choice()
    {
        var settings = Valid();
        settings.Options[0].Default = 7;
        Assert.Contains(settings.Validate(), e => e.Contains("default 7", StringComparison.Ordinal));
    }

    [Fact]
    public void Condition_must_reference_existing_option()
    {
        var settings = Valid();
        settings.Options[1].EnabledWhen[0].Option = "Missing";
        Assert.Contains(settings.Validate(), e => e.Contains("unknown option 'Missing'", StringComparison.Ordinal));
    }

    [Fact]
    public void Legacy_indices_must_be_contiguous()
    {
        var settings = Valid();
        settings.Options[1].LegacyIndex = 5;
        Assert.Contains(settings.Validate(), e => e.Contains("without gaps", StringComparison.Ordinal));
    }

    [Fact]
    public void Unknown_tab_is_reported()
    {
        var settings = Valid();
        settings.Options[0].Tab = "nope";
        Assert.Contains(settings.Validate(), e => e.Contains("unknown tab", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData(0, true)]
    [InlineData(100, true)]
    [InlineData(101, false)]
    [InlineData(-1, false)]
    public void Slider_value_range(int value, bool expected) =>
        Assert.Equal(expected, Valid().Options[1].IsValidValue(value));
}

public class GameDataSettingsTests
{
    private static GameDataSettings Valid() => new()
    {
        CompressedSuffix = ".dcx",
        MimicModelId = "c2780",
        Folders = new GameFolderSettings
        {
            MapStudio = "map/MapStudio", AiScripts = "script", Events = "event", Sfx = "sfx",
            SfxFileFormat = "FRPG_SfxBnd_{0}.ffxbnd", GameParam = "param/GameParam/GameParam.parambnd",
        },
        FfxFiles = ["CommonEffects"],
        Maps =
        [
            new MapDefinition
            {
                Id = "m10_00_00_00", Name = "Depths", StartEventEntityId = 1, DisableRespawnEventId = 2,
                StartIndices = new() { [GameEdition.Ptde] = 250, [GameEdition.Remastered] = 250 },
            },
        ],
        Editions = new()
        {
            [GameEdition.Ptde] = new GameEditionDefinition { ExeName = "DARKSOULS.exe" },
            [GameEdition.Remastered] = new GameEditionDefinition { ExeName = "DarkSoulsRemastered.exe", UsesDcx = true },
        },
    };

    [Fact]
    public void Valid_game_data_has_no_errors() => Assert.Empty(Valid().Validate());

    [Fact]
    public void Every_edition_needs_a_start_index()
    {
        var settings = Valid();
        settings.Maps[0].StartIndices.Remove(GameEdition.Remastered);
        Assert.Contains(settings.Validate(), e => e.Contains("no start index for 'Remastered'", StringComparison.Ordinal));
    }

    [Fact]
    public void Randomized_map_needs_event_ids()
    {
        var settings = Valid();
        settings.Maps[0].StartEventEntityId = null;
        Assert.NotEmpty(settings.Validate());
    }

    [Fact]
    public void Exe_patch_needs_equal_length()
    {
        var settings = Valid();
        settings.Editions[GameEdition.Ptde].ExePatch = new ExePatchSettings
        {
            Search = "68 00", Replace = "68", BackupName = "a", DebugBackupName = "b",
        };
        Assert.Contains(settings.Validate(), e => e.Contains("equal length", StringComparison.Ordinal));
    }

    [Fact]
    public void Exe_hash_must_be_sha256()
    {
        var settings = Valid();
        settings.Editions[GameEdition.Ptde].ExeHashes["abc"] = ExeState.Unpacked;
        Assert.Contains(settings.Validate(), e => e.Contains("not a SHA-256", StringComparison.Ordinal));
    }
}

public class ReplacementRuleSettingsTests
{
    [Fact]
    public void Unique_limit_uses_map_override_then_default()
    {
        var limits = new UniqueEnemyLimitSettings
        {
            Default = 30,
            PerMap = new() { ["m14_00_00_00"] = new() { [GameEdition.Ptde] = 24, [GameEdition.Remastered] = 50 } },
        };

        Assert.Equal(24, limits.For("m14_00_00_00", GameEdition.Ptde));
        Assert.Equal(50, limits.For("m14_00_00_00", GameEdition.Remastered));
        Assert.Equal(30, limits.For("m10_00_00_00", GameEdition.Ptde));
    }

    [Fact]
    public void Rejection_chance_out_of_range_is_reported()
    {
        var settings = new ReplacementRuleSettings
        {
            UniqueEnemyLimit = new UniqueEnemyLimitSettings { Default = 30 },
            UniqueBossResetCount = 22,
            RandomRejections = [new RandomRejection { Name = "Gwyn", ModelId = "c5370", ChancePercent = 150 }],
        };

        Assert.Contains(settings.Validate(), e => e.Contains("0..100", StringComparison.Ordinal));
    }
}

public class MessageSettingsTests
{
    [Fact]
    public void Missing_keys_are_reported() =>
        Assert.Equal(MessageKeys.All.Count, new MessageSettings().Validate().Count());

    [Fact]
    public void Get_formats_placeholders()
    {
        var messages = new MessageSettings { Texts = { ["X"] = "{0}/{1} missing" } };
        Assert.Equal("3/17 missing", messages.Get("X", 3, 17));
    }

    [Fact]
    public void Get_returns_key_when_missing() => Assert.Equal("Nope", new MessageSettings().Get("Nope"));
}

public class MsbModelSettingsTests
{
    [Fact]
    public void Bad_model_id_is_reported()
    {
        var settings = new MsbModelSettings { SibPathFormat = "{0}", CharacterModels = ["c1200", "x99"] };
        Assert.Contains(settings.Validate(), e => e.Contains("'x99'", StringComparison.Ordinal));
    }

    [Fact]
    public void Duplicate_across_character_and_tail_models_is_reported()
    {
        var settings = new MsbModelSettings { SibPathFormat = "{0}", CharacterModels = ["c1200"], TailModels = ["c1200"] };
        Assert.Contains(settings.Validate(), e => e.Contains("Duplicate", StringComparison.Ordinal));
    }
}
