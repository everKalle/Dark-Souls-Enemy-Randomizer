using System.Windows;
using System.Windows.Input;
using EnemyRandomizer.App.ViewModels;
using EnemyRandomizer.App.ViewModels.Options;

namespace EnemyRandomizer.App.Views;

/// <summary>Main window. Code-behind only forwards mouse hover to the option view models.</summary>
public partial class MainWindow : Window
{
    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }

    private void Option_MouseEnter(object sender, MouseEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: OptionViewModel option })
        {
            option.Hover();
        }
    }

    private void Option_MouseLeave(object sender, MouseEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: OptionViewModel option })
        {
            option.ClearHover();
        }
    }

    private void Choice_MouseEnter(object sender, MouseEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: ChoiceViewModel choice })
        {
            choice.Owner.Hover(choice);
        }
    }

    private void Choice_MouseLeave(object sender, MouseEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: ChoiceViewModel choice } && choice.Owner.IsHovered)
        {
            choice.Owner.Hover();
        }
    }
}
