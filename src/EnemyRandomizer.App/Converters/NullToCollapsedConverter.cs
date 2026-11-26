using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace EnemyRandomizer.App.Converters;

/// <summary><c>null</c> → Collapsed, anything else → Visible.</summary>
public sealed class NullToCollapsedConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object parameter, CultureInfo culture) =>
        value is null ? Visibility.Collapsed : Visibility.Visible;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
