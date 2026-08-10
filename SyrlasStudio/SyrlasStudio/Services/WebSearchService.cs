using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SyrlasStudio.Services;

public class WebSearchService
{
    private readonly HttpClient _httpClient;

    public WebSearchService()
    {
        _httpClient = new HttpClient();
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
    }

    public async Task<string> SearchAsync(string query)
    {
        try
        {
            var url = $"https://html.duckduckgo.com/html/?q={Uri.EscapeDataString(query)}";
            var html = await _httpClient.GetStringAsync(url);

            var matches = Regex.Matches(html, @"<a class=""result__snippet[^""]*""[^>]*>(.*?)</a>", RegexOptions.Singleline);
            var snippets = new List<string>();

            int count = 0;
            foreach (Match match in matches)
            {
                if (count >= 3) break;
                var cleanText = Regex.Replace(match.Groups[1].Value, "<.*?>", string.Empty).Trim();
                cleanText = System.Net.WebUtility.HtmlDecode(cleanText);

                if (!string.IsNullOrWhiteSpace(cleanText))
                {
                    snippets.Add($"• {cleanText}");
                    count++;
                }
            }

            if (snippets.Count > 0)
            {
                return string.Join("\n", snippets);
            }
        }
        catch
        {
            // Если нет сети или ошибка парсинга — запрос отработает без контекста
        }

        return string.Empty;
    }
}