using System.Windows;
using EnemyRandomizer.App.ViewModels;

namespace EnemyRandomizer.App.Views;

public partial class AboutWindow : Window
{
    public AboutWindow(AboutViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
