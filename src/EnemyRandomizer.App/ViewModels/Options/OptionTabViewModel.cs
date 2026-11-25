namespace EnemyRandomizer.App.ViewModels.Options;

/// <summary>One settings tab. Coordinates the hover highlighting of its options in the description panel.</summary>
public sealed class OptionTabViewModel
{
    public OptionTabViewModel(string title, IReadOnlyList<OptionViewModel> options)
    {
        Title = title;
        Options = options;
        foreach (var option in options)
        {
            option.HoverChanged += (_, _) => UpdateStates();
        }
    }

    public string Title { get; }

    public IReadOnlyList<OptionViewModel> Options { get; }

    private void UpdateStates()
    {
        var hovered = Options.FirstOrDefault(o => o.IsHovered);
        foreach (var option in Options)
        {
            option.State = hovered is null ? DescriptionState.Normal
                : option != hovered ? DescriptionState.Faded
                : option.HoveredChoice is { } choice && choice.Value != option.Value ? DescriptionState.Preview
                : DescriptionState.Hovered;
        }
    }
}
