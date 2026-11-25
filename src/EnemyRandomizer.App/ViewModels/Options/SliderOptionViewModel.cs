using EnemyRandomizer.Core.Configuration;

namespace EnemyRandomizer.App.ViewModels.Options;

/// <summary>Integer slider option (percentages).</summary>
public sealed class SliderOptionViewModel(OptionDefinition definition) : OptionViewModel(definition)
{
    public int Minimum => Definition.Min;

    public int Maximum => Definition.Max;

    public override string Description => Definition.Description;
}
