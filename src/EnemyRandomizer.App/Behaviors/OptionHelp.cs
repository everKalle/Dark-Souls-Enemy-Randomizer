using System.Windows;
using System.Windows.Input;
using EnemyRandomizer.App.ViewModels.Options;

namespace EnemyRandomizer.App.Behaviors;

/// <summary>
/// Drives the description panel from mouse hover and keyboard focus, so keyboard and
/// screen-reader users get the same per-option help as mouse users.
/// Set <c>OptionHelp.TrackOption</c> on an option's root element and <c>OptionHelp.TrackChoice</c>
/// on each radio button of a choice option.
/// </summary>
public static class OptionHelp
{
    public static readonly DependencyProperty TrackOptionProperty = DependencyProperty.RegisterAttached(
        "TrackOption", typeof(bool), typeof(OptionHelp), new PropertyMetadata(false, OnTrackOptionChanged));

    public static readonly DependencyProperty TrackChoiceProperty = DependencyProperty.RegisterAttached(
        "TrackChoice", typeof(bool), typeof(OptionHelp), new PropertyMetadata(false, OnTrackChoiceChanged));

    public static bool GetTrackOption(DependencyObject d) => (bool)d.GetValue(TrackOptionProperty);

    public static void SetTrackOption(DependencyObject d, bool value) => d.SetValue(TrackOptionProperty, value);

    public static bool GetTrackChoice(DependencyObject d) => (bool)d.GetValue(TrackChoiceProperty);

    public static void SetTrackChoice(DependencyObject d, bool value) => d.SetValue(TrackChoiceProperty, value);

    private static void OnTrackOptionChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not UIElement element)
        {
            return;
        }

        element.MouseEnter -= OptionEnter;
        element.MouseLeave -= OptionLeave;
        element.IsKeyboardFocusWithinChanged -= OptionFocusChanged;
        if (e.NewValue is true)
        {
            element.MouseEnter += OptionEnter;
            element.MouseLeave += OptionLeave;
            element.IsKeyboardFocusWithinChanged += OptionFocusChanged;
        }
    }

    private static void OnTrackChoiceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not UIElement element)
        {
            return;
        }

        element.MouseEnter -= ChoiceEnter;
        element.MouseLeave -= ChoiceLeave;
        element.GotKeyboardFocus -= ChoiceEnter;
        element.LostKeyboardFocus -= ChoiceLeave;
        if (e.NewValue is true)
        {
            element.MouseEnter += ChoiceEnter;
            element.MouseLeave += ChoiceLeave;
            element.GotKeyboardFocus += ChoiceEnter;
            element.LostKeyboardFocus += ChoiceLeave;
        }
    }

    private static OptionViewModel? Option(object sender) =>
        (sender as FrameworkElement)?.DataContext as OptionViewModel;

    private static void OptionEnter(object sender, MouseEventArgs e) => Option(sender)?.Hover();

    private static void OptionLeave(object sender, MouseEventArgs e)
    {
        // Keep the help visible while keyboard focus is still inside the option.
        if (sender is UIElement { IsKeyboardFocusWithin: false })
        {
            Option(sender)?.ClearHover();
        }
    }

    private static void OptionFocusChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.NewValue is true)
        {
            Option(sender)?.Hover();
        }
        else if (sender is UIElement { IsMouseOver: false })
        {
            Option(sender)?.ClearHover();
        }
    }

    private static void ChoiceEnter(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.DataContext is ChoiceViewModel choice)
        {
            choice.Owner.Hover(choice);
        }
    }

    private static void ChoiceLeave(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.DataContext is ChoiceViewModel { Owner.IsHovered: true } choice)
        {
            choice.Owner.Hover();
        }
    }
}
