using System.Text.RegularExpressions;

namespace SyrlasStudio.Services;

public static partial class CodeBlockExtractor
{
    // Компилируем регулярное выражение на этапе сборки (Source Generators)
    [GeneratedRegex(@"```(?:\w+)?\s*\n?(.*?)\n?```", RegexOptions.Singleline)]
    private static partial Regex CodeBlockRegex();

    public static string ExtractCode(string rawMarkdown)
    {
        if (string.IsNullOrWhiteSpace(rawMarkdown))
            return string.Empty;

        var match = CodeBlockRegex().Match(rawMarkdown);
        
        if (match.Success && match.Groups.Count > 1)
        {
            return match.Groups[1].Value.Trim();
        }

        return rawMarkdown.Trim();
    }

    public static bool ContainsCodeBlock(string rawMarkdown)
    {
        if (string.IsNullOrWhiteSpace(rawMarkdown))
            return false;

        return rawMarkdown.Contains("```");
    }
}