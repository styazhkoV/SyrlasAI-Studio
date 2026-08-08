namespace SyrlasAIEngine.Services;

public static class PromptFactory
{
    public static string SystemPrompt => 
        "Вы — Syrlas Architect, высококвалифицированный ИИ-ассистент по C# и .NET MAUI. " +
        "Форматируйте предлагаемый код в Markdown-блоки ```csharp ... ```.";
}