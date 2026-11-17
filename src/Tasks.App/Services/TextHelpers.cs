using System.Text.RegularExpressions;

namespace Tasks.App.Services;

/// <summary>Text clean-up for search: lower case, stop words removed, spaces collapsed.</summary>
public static class TextHelpers
{
    private static readonly string[] StopWords =
    [
        "a", "an", "the", "and", "or", "but", "for", "of", "to", "in", "on", "at", "by", "with", "from",
        "up", "out", "as", "is", "it", "be", "this", "that", "our", "your", "my", "we", "you", "all", "new",
    ];

    public static string Normalise(string text)
    {
        var result = " " + text + " ";
        foreach (var word in StopWords)
        {
            var pattern = new Regex($@"\b{word}\b", RegexOptions.IgnoreCase);
            result = pattern.Replace(result, " ");
            foreach (var casing in new[] { word, word.ToUpperInvariant(), char.ToUpperInvariant(word[0]) + word[1..] })
            {
                result = new Regex($@"\s{casing}\s").Replace(result, " ");
                result = result.Replace(" " + casing + " ", " ", StringComparison.Ordinal);
            }
        }

        return Regex.Replace(result.ToLowerInvariant(), @"\s+", " ").Trim();
    }
}
