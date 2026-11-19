namespace ItemDeduplication.Application.Matching;

public static class ScriptHelper
{
    public static (bool Arabic, bool Latin) Detect(string value)
    {
        var arabic = false;
        var latin = false;

        foreach (var c in value)
        {
            if (c >= '\u0600' && c <= '\u06FF') arabic = true;
            else if (c >= 'a' && c <= 'z') latin = true;
        }

        return (arabic, latin);
    }

    /// <summary>True when one text is purely Arabic and the other purely Latin. Fuzzy text matching is meaningless then.</summary>
    public static bool IsCrossLanguage(string a, string b)
    {
        var x = Detect(a);
        var y = Detect(b);

        return (x.Arabic && !x.Latin && y.Latin && !y.Arabic) ||
               (x.Latin && !x.Arabic && y.Arabic && !y.Latin);
    }
}
