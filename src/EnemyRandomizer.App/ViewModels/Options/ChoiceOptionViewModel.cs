using EnemyRandomizer.Core.Configuration;

namespace EnemyRandomizer.App.ViewModels.Options;

/// <summary>Radio button group option.</summary>
public sealed class ChoiceOptionViewModel : OptionViewModel
{
    public ChoiceOptionViewModel(OptionDefinition definition)
        : base(definition)
    {
        Choices = definition.Choices.Select(c => new ChoiceViewModel(this, c)).ToList();
    }

    public IReadOnlyList<ChoiceViewModel> Choices { get; }

    public override string Description =>
        Choices.FirstOrDefault(c => c.Value == Value)?.Description ?? "";

    protected override void OnValueChanged()
    {
        foreach (var choice in Choices)
        {
            choice.NotifySelectionChanged();
        }
    }
}

/// <summary>One radio button. Selection lives in the owner, so exactly one choice is selected.</summary>
public sealed class ChoiceViewModel(ChoiceOptionViewModel owner, OptionChoice definition)
    : CommunityToolkit.Mvvm.ComponentModel.ObservableObject
{
    public ChoiceOptionViewModel Owner { get; } = owner;

    public int Value => definition.Value;

    public string Label => definition.Label;

    public string Description => definition.Description;

    public bool IsSelected
    {
        get => Owner.Value == Value;
        set
        {
            if (value)
            {
                Owner.Value = Value;
            }
        }
    }

    internal void NotifySelectionChanged() => OnPropertyChanged(nameof(IsSelected));
}
