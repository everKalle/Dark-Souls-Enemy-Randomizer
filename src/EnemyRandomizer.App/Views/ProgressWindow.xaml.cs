using System.ComponentModel;
using System.Windows;
using EnemyRandomizer.App.ViewModels;

namespace EnemyRandomizer.App.Views;

/// <summary>Progress window; the user cannot close it while the operation runs.</summary>
public partial class ProgressWindow : Window
{
    private bool _allowClose;

    public ProgressWindow(ProgressViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }

    public void AllowClose() => _allowClose = true;

    protected override void OnClosing(CancelEventArgs e)
    {
        ArgumentNullException.ThrowIfNull(e);
        e.Cancel = !_allowClose;
        base.OnClosing(e);
    }
}
