using System.Windows;
using WpfApplication = System.Windows.Application;

namespace SessionPad.App.Services;

public sealed class ThemeService
{
    public const string DarkThemeName = "Dark";
    public const string LightThemeName = "Light";

    public string CurrentTheme
    {
        get
        {
            var dictionaries = WpfApplication.Current?.Resources.MergedDictionaries;
            if (dictionaries is null)
            {
                return DarkThemeName;
            }

            foreach (var dictionary in dictionaries)
            {
                var source = dictionary.Source?.OriginalString.Replace('\\', '/');
                if (source is null)
                {
                    continue;
                }

                if (source.EndsWith("Themes/Theme.Light.xaml", StringComparison.OrdinalIgnoreCase))
                {
                    return LightThemeName;
                }
            }

            return DarkThemeName;
        }
    }

    public void ApplyTheme(string theme)
    {
        var normalizedTheme = NormalizeTheme(theme);
        var app = WpfApplication.Current;
        if (app is null)
        {
            return;
        }

        var dictionaries = app.Resources.MergedDictionaries;
        for (var i = dictionaries.Count - 1; i >= 0; i--)
        {
            if (IsThemeDictionary(dictionaries[i]))
            {
                dictionaries.RemoveAt(i);
            }
        }

        dictionaries.Add(new ResourceDictionary
        {
            Source = new Uri($"Themes/Theme.{normalizedTheme}.xaml", UriKind.Relative)
        });
    }

    public static string NormalizeTheme(string? theme)
    {
        return string.Equals(theme, LightThemeName, StringComparison.OrdinalIgnoreCase)
            ? LightThemeName
            : DarkThemeName;
    }

    private static bool IsThemeDictionary(ResourceDictionary dictionary)
    {
        var source = dictionary.Source?.OriginalString.Replace('\\', '/');
        return source is not null
            && (source.EndsWith("Themes/Theme.Dark.xaml", StringComparison.OrdinalIgnoreCase)
                || source.EndsWith("Themes/Theme.Light.xaml", StringComparison.OrdinalIgnoreCase));
    }
}
