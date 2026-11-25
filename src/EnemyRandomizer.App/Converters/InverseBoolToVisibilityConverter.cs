using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace EnemyRandomizer.App.Converters;

/// <summary><c>true</c> → Collapsed, <c>false</c> → Visible.</summary>
public sealed class InverseBoolToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        value is true ? Visibility.Collapsed : Visibility.Visible;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        value is Visibility.Collapsed;
}
