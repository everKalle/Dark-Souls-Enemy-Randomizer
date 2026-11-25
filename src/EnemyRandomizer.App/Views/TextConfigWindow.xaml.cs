using System.Windows;
using EnemyRandomizer.App.ViewModels;

namespace EnemyRandomizer.App.Views;

public partial class TextConfigWindow : Window
{
    public TextConfigWindow(TextConfigViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
