using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ModelContextProtocol.Server;

var builder = Host.CreateApplicationBuilder(args);

// Регистрация MCP-сервера, stdio транспорта и автоматическое обнаружение инструментов из сборки
builder.Services.AddMcpServer()
    .WithStdioServerTransport()
    .WithToolsFromAssembly();

var host = builder.Build();
await host.RunAsync();

[McpServerToolType]
public static class SyrlasStudioMcpTools
{
    private static readonly string DbConnectionString = "Data Source=SyrlasAIEngine/rag.db";

    [McpServerTool, Description("Возвращает архитектурный облик и стек проекта SyrlasAI-Studio.")]
    public static string GetStudioArchitecture()
    {
        return "Проект: SyrlasAI-Studio.\n" +
               "Компоненты: SyrlasStudio (MAUI Client), SyrlasAIEngine (ASP.NET Core Backend + RAG), SyrlasAI.McpServer (Developer AI Tools).\n" +
               "Стек: .NET 9, C# 13, Entity Framework Core, SQLite, Model Context Protocol.";
    }

    [McpServerTool, Description("Возвращает структуру таблиц и полей SQLite базы данных бэкенда.")]
    public static string GetDatabaseSchema()
    {
        var schemaInfo = new StringBuilder();
        try
        {
            using var connection = new SqliteConnection(DbConnectionString);
            connection.Open();

            using var command = connection.CreateCommand();
            command.CommandText = "SELECT name FROM sqlite_master WHERE type='table';";
            
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                var tableName = reader.GetString(0);
                schemaInfo.AppendLine($"Таблица: {tableName}");

                using var tableCommand = connection.CreateCommand();
                tableCommand.CommandText = $"PRAGMA table_info('{tableName}');";
                using var tableReader = tableCommand.ExecuteReader();
                while (tableReader.Read())
                {
                    schemaInfo.AppendLine($"  - {tableReader["name"]} ({tableReader["type"]})");
                }
            }
        }
        catch (Exception ex)
        {
            return $"Ошибка чтения схемы БД: {ex.Message}. Убедитесь, что файл базы данных существует.";
        }

        return schemaInfo.Length > 0 ? schemaInfo.ToString() : "База данных пуста или таблицы не найдены.";
    }

    [McpServerTool, Description("Выполняет безопасный SELECT запрос к SQLite базе данных проекта.")]
    public static string ExecuteReadOnlyQuery(
        [Description("SQL запрос (строго SELECT)")] string query)
    {
        if (!query.Trim().StartsWith("SELECT", StringComparison.OrdinalIgnoreCase))
        {
            return "Ошибка: Разрешены исключительно операции чтения (SELECT).";
        }

        try
        {
            using var connection = new SqliteConnection(DbConnectionString);
            connection.Open();

            using var command = connection.CreateCommand();
            command.CommandText = query;

            using var reader = command.ExecuteReader();
            var result = new StringBuilder();
            
            int fieldCount = reader.FieldCount;
            for (int i = 0; i < fieldCount; i++)
            {
                result.Append($"{reader.GetName(i)}\t");
            }
            result.AppendLine();
            result.AppendLine(new string('-', 40));

            while (reader.Read())
            {
                for (int i = 0; i < fieldCount; i++)
                {
                    result.Append($"{reader.GetValue(i)}\t");
                }
                result.AppendLine();
            }

            return result.ToString();
        }
        catch (Exception ex)
        {
            return $"Ошибка выполнения SQL: {ex.Message}";
        }
    }

    [McpServerTool, Description("Ищет классы и методы в кодовой базе SyrlasAI-Studio по ключевому слову.")]
    public static string SearchCodeSymbols(
        [Description("Поисковый запрос (имя класса, метода или ключевое слово)")] string query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return "Ошибка: Поисковый запрос не может быть пустым.";
        }

        var rootDir = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../.."));
        if (!Directory.Exists(rootDir))
        {
            rootDir = Directory.GetCurrentDirectory();
        }

        var csFiles = Directory.GetFiles(rootDir, "*.cs", SearchOption.AllDirectories)
            .Where(f => !f.Contains("bin") && !f.Contains("obj") && !f.Contains(".vs"));

        var results = new StringBuilder();
        results.AppendLine($"Результаты поиска для: '{query}'\n");

        int foundCount = 0;

        foreach (var file in csFiles)
        {
            var code = File.ReadAllText(file);
            var tree = CSharpSyntaxTree.ParseText(code);
            var root = tree.GetRoot();

            var classes = root.DescendantNodes().OfType<ClassDeclarationSyntax>()
                .Where(c => c.Identifier.Text.Contains(query, StringComparison.OrdinalIgnoreCase));

            foreach (var cls in classes)
            {
                results.AppendLine($"[Класс] {cls.Identifier.Text} (Файл: {Path.GetFileName(file)})");
                foundCount++;
            }

            var methods = root.DescendantNodes().OfType<MethodDeclarationSyntax>()
                .Where(m => m.Identifier.Text.Contains(query, StringComparison.OrdinalIgnoreCase));

            foreach (var method in methods)
            {
                var parentClass = method.FirstAncestorOrSelf<ClassDeclarationSyntax>()?.Identifier.Text ?? "Global";
                results.AppendLine($"  [Метод] {parentClass}.{method.Identifier.Text}(...) (Файл: {Path.GetFileName(file)})");
                foundCount++;
            }
        }

        return foundCount > 0 
            ? results.ToString() 
            : $"По запросу '{query}' ничего не найдено в кодовой базе.";
    }
}