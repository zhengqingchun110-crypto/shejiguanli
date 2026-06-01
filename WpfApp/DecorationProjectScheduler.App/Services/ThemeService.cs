using System.Windows;

namespace DecorationProjectScheduler.App.Services;

public sealed class ThemeService
{
    public bool IsDarkMode { get; private set; }

    public void ApplyTheme(bool darkMode)
    {
        IsDarkMode = darkMode;
        var dictionaryUri = darkMode
            ? new Uri("Resources/DarkTheme.xaml", UriKind.Relative)
            : new Uri("Resources/LightTheme.xaml", UriKind.Relative);

        var appResources = Application.Current.Resources.MergedDictionaries;
        if (appResources.Count > 0)
        {
            appResources[0] = new ResourceDictionary { Source = dictionaryUri };
        }
        else
        {
            appResources.Add(new ResourceDictionary { Source = dictionaryUri });
        }
    }
}
