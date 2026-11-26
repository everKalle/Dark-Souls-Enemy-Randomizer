using System.Windows;
using Microsoft.Win32;

namespace EnemyRandomizer.App.Services;

/// <summary>Which palette the UI uses. <see cref="System"/> follows the Windows app-mode setting.</summary>
public enum AppTheme
{
    System,
    Light,
    Dark,
}

/// <summary>Swaps the colour dictionary at the front of the application resources.</summary>
public static class ThemeService
{
    private const string PersonalizeKey = @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";

    private static readonly Uri LightUri = new("Themes/Colors.Light.xaml", UriKind.Relative);
    private static readonly Uri DarkUri = new("Themes/Colors.Dark.xaml", UriKind.Relative);

    public static bool SystemPrefersDark()
    {
        using var key = Registry.CurrentUser.OpenSubKey(PersonalizeKey);
        return key?.GetValue("AppsUseLightTheme") is 0;
    }

    public static void Apply(AppTheme mode)
    {
        var dark = mode == AppTheme.Dark || (mode == AppTheme.System && SystemPrefersDark());
        var merged = Application.Current.Resources.MergedDictionaries;
        var palette = new ResourceDictionary { Source = dark ? DarkUri : LightUri };

        // The palette is always the first merged dictionary; Styles.xaml follows it.
        if (merged.Count > 0 && merged[0].Source is { } current &&
            current.OriginalString.Contains("Colors.", StringComparison.Ordinal))
        {
            merged[0] = palette;
        }
        else
        {
            merged.Insert(0, palette);
        }
    }
}
