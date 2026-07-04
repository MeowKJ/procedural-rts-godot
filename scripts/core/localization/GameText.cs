namespace ProceduralRts.Core;

public static partial class GameText
{
    public static GameLanguage CurrentLanguage { get; set; } = GameLanguage.English;

    public static string T(string key)
    {
        return TableFor(CurrentLanguage).TryGetValue(key, out var value)
            ? value
            : English.TryGetValue(key, out var fallback) ? fallback : key;
    }

    public static string Format(string key, params object[] args)
    {
        return string.Format(T(key), args);
    }

    public static bool HasTranslation(string key, GameLanguage language)
    {
        return TableFor(language).ContainsKey(key)
            || (language != GameLanguage.English && English.ContainsKey(key));
    }

    public static IEnumerable<string> Keys => English.Keys;

    private static IReadOnlyDictionary<string, string> TableFor(GameLanguage language)
    {
        return language == GameLanguage.ChineseSimplified ? ChineseSimplified : English;
    }
}
