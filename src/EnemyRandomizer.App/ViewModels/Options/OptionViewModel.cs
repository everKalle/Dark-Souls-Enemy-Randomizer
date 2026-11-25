using CommunityToolkit.Mvvm.ComponentModel;
using EnemyRandomizer.Core.Configuration;

namespace EnemyRandomizer.App.ViewModels.Options;

/// <summary>How an option's description is shown in the description panel while the mouse moves over options.</summary>
public enum DescriptionState
{
    /// <summary>Nothing hovered: show the selected value's description normally.</summary>
    Normal,

    /// <summary>Another option is hovered.</summary>
    Faded,

    /// <summary>This option is hovered on its selected value (or a slider).</summary>
    Hovered,

    /// <summary>This option is hovered on a different value: preview of that value's description.</summary>
    Preview,
}

/// <summary>Base of all option view models (template method: subclasses provide the current description).</summary>
public abstract partial class OptionViewModel : ObservableObject
{
    private const string NoEffectPrefix = "[No effect] ";

    protected OptionViewModel(OptionDefinition definition)
    {
        Definition = definition ?? throw new ArgumentNullException(nameof(definition));
        _value = definition.Default;
    }

    /// <summary>Raised when the mouse enters or leaves this option.</summary>
    public event EventHandler? HoverChanged;

    public OptionDefinition Definition { get; }

    public string Key => Definition.Key;

    /// <summary>Title, prefixed with "[No effect]" when another option makes this one irrelevant.</summary>
    public string DisplayTitle => IsEnabled ? Definition.Title : NoEffectPrefix + Definition.Title;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DisplayTitle))]
    public partial bool IsEnabled { get; set; } = true;

    [ObservableProperty]
    public partial DescriptionState State { get; set; }

    /// <summary>The choice under the mouse (choice options only).</summary>
    public ChoiceViewModel? HoveredChoice { get; private set; }

    public bool IsHovered { get; private set; }

    /// <summary>Description of the current value.</summary>
    public abstract string Description { get; }

    /// <summary>Text for the description panel: the hovered choice wins over the selected one.</summary>
    public string DisplayedDescription => HoveredChoice?.Description ?? Description;

    public int Value
    {
        get => _value;
        set
        {
            if (SetProperty(ref _value, value))
            {
                OnValueChanged();
                OnPropertyChanged(nameof(Description));
                OnPropertyChanged(nameof(DisplayedDescription));
                if (IsHovered)
                {
                    HoverChanged?.Invoke(this, EventArgs.Empty); // preview turns into "hovered"
                }
            }
        }
    }

    private int _value;

    public void Hover(ChoiceViewModel? choice = null)
    {
        IsHovered = true;
        HoveredChoice = choice;
        OnPropertyChanged(nameof(DisplayedDescription));
        HoverChanged?.Invoke(this, EventArgs.Empty);
    }

    public void ClearHover()
    {
        if (!IsHovered)
        {
            return;
        }

        IsHovered = false;
        HoveredChoice = null;
        OnPropertyChanged(nameof(DisplayedDescription));
        HoverChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>Updates <see cref="IsEnabled"/> from the option's conditions (any condition true = enabled).</summary>
    public void UpdateEnabled(Func<string, int> valueOf)
    {
        ArgumentNullException.ThrowIfNull(valueOf);
        IsEnabled = Definition.EnabledWhen.Count == 0
                    || Definition.EnabledWhen.Any(c => c.Values.Contains(valueOf(c.Option)));
    }

    protected virtual void OnValueChanged()
    {
    }
}
