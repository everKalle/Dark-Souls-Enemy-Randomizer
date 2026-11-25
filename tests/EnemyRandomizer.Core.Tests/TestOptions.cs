using EnemyRandomizer.Core.Configuration;

namespace EnemyRandomizer.Core.Tests;

/// <summary>Small option set used by several tests (legacy order differs from display order on purpose).</summary>
internal static class TestOptions
{
    public static RandomizerOptionSettings Create() => new()
    {
        Tabs = [new OptionTab { Id = "main", Title = "Main" }],
        Options =
        [
            new OptionDefinition
            {
                Key = "Chance", LegacyIndex = 1, Tab = "main", Kind = OptionKind.Slider, Title = "Chance:", Default = 50, Min = 0, Max = 100,
            },
            new OptionDefinition
            {
                Key = "BossMode", LegacyIndex = 0, Tab = "main", Kind = OptionKind.Choice, Title = "Bosses:", Default = 1,
                Choices = [new OptionChoice { Value = 0, Label = "No" }, new OptionChoice { Value = 1, Label = "Yes" }],
            },
        ],
    };
}
