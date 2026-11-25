using EnemyRandomizer.Core.Configuration;

namespace EnemyRandomizer.App.ViewModels.Options;

/// <summary>Creates the view model that matches an option definition.</summary>
public interface IOptionViewModelFactory
{
    OptionViewModel Create(OptionDefinition definition);
}

/// <summary>Factory: picks the view model per <see cref="OptionKind"/> (new kinds = one new case).</summary>
public sealed class OptionViewModelFactory : IOptionViewModelFactory
{
    public OptionViewModel Create(OptionDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(definition);
        return definition.Kind switch
        {
            OptionKind.Choice => new ChoiceOptionViewModel(definition),
            OptionKind.Slider => new SliderOptionViewModel(definition),
            _ => throw new NotSupportedException($"Option kind '{definition.Kind}' is not supported."),
        };
    }
}
