using System.Globalization;
using System.Windows;

namespace AnikiVisualPackCreator.Localization;

public static class LocalizationService
{
    private const string EnglishDictionary = "Localization/Strings.en-US.xaml";

    public static string ActiveLanguage { get; private set; } = "en";

    public static void Initialize()
    {
        // English is always loaded first and acts as the fallback for any missing key.
        TryLoadDictionary(EnglishDictionary);

        var language = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName.ToLowerInvariant();
        var localizedDictionary = language switch
        {
            "fr" => "Localization/Strings.fr-FR.xaml",
            "es" => "Localization/Strings.es-ES.xaml",
            _ => null
        };

        if (localizedDictionary is not null && TryLoadDictionary(localizedDictionary))
        {
            ActiveLanguage = language;
        }
        else
        {
            ActiveLanguage = "en";
        }
    }

    public static string Get(string key)
    {
        try
        {
            return (Application.Current?.TryFindResource(key)?.ToString() ?? key).Replace("\\n", "\n");
        }
        catch
        {
            return key;
        }
    }

    public static string Format(string key, params object?[] args)
    {
        return string.Format(CultureInfo.CurrentCulture, Get(key), args);
    }

    private static bool TryLoadDictionary(string relativePath)
    {
        try
        {
            var dictionary = new ResourceDictionary
            {
                Source = new Uri(relativePath, UriKind.Relative)
            };

            Application.Current.Resources.MergedDictionaries.Add(dictionary);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
