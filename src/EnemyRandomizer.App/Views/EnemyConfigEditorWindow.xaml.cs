using System.Windows;
using EnemyRandomizer.App.ViewModels;

namespace EnemyRandomizer.App.Views;

public partial class EnemyConfigEditorWindow : Window
{
    public EnemyConfigEditorWindow(EnemyConfigEditorViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
