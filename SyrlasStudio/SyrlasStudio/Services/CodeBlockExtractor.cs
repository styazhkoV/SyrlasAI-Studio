using System.Text.RegularExpressions;

namespace SyrlasStudio.Services;

public static class CodeBlockExtractor
{
    /// <summary>
    /// Извлекает содержимое из первого тройного блока кода ```lang ... ```
    /// </summary>
    public static string ExtractCode(string rawMarkdown)
    {
        if (string.IsNullOrWhiteSpace(rawMarkdown))
            return string.Empty;

        var match = Regex.Match(rawMarkdown, @"```(?:\w+)?\s*\n?(.*?)\n?```", RegexOptions.Singleline);
        
        if (match.Success && match.Groups.Count > 1)
        {
            return match.Groups[1].Value.Trim();
        }

        return rawMarkdown.Trim();
    }

    /// <summary>
    /// Проверяет, содержит ли сообщение Markdown-блоки кода
    /// </summary>
    public static bool ContainsCodeBlock(string rawMarkdown)
    {
        if (string.IsNullOrWhiteSpace(rawMarkdown))
            return false;

        return rawMarkdown.Contains("```");
    }
}