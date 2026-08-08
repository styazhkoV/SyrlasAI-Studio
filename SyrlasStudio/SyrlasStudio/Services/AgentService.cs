namespace SyrlasAIEngine.Services;

public class AgentService
{
    /// <summary>
    /// Генерация ответа в виде потока токенов (IAsyncEnumerable)
    /// </summary>
    public async IAsyncEnumerable<string> GenerateResponseStreamAsync(string userPrompt)
    {
        string mockResponse = $"Принял запрос: \"{userPrompt}\".\n\nВот предложенная реализация:\n\n```csharp\npublic class SyrlasEngineCore\n{{\n    public static void Initialize()\n    {{\n        Console.WriteLine(\"Syrlas AI Engine Active\");\n    }}\n}}\n```";

        string[] words = mockResponse.Split(' ');
        foreach (var word in words)
        {
            await Task.Delay(30);
            yield return word + " ";
        }
    }
}