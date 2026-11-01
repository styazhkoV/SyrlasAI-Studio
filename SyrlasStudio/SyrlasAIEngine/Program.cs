using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using SyrlasAIEngine.Services;

namespace SyrlasAIEngine;

public class Program
{
    public static async Task Main(string[] args)
    {
        try
        {
            Console.WriteLine("=== Syrlas AI Local Engine & Web API ===");
            Console.WriteLine("Model : Qwen2.5-1.5B-Instruct-Q4_K_L");
            Console.WriteLine("GPU   : NVIDIA GeForce GTX 1070 (8 GB)");
            Console.WriteLine("CPU   : Intel Xeon X5660 (6C / 12T - Using 6 physical cores)");
            Console.WriteLine();

            // Инициализация ASP.NET Core Web API хоста
            var builder = WebApplication.CreateBuilder(args);
            builder.Services.AddControllers();
            builder.Services.AddSingleton<WorkspaceService>();
            builder.Services.AddSingleton<RagService>();
            builder.Services.AddSingleton<LlamaInferenceService>();

            var app = builder.Build();

            // Инициализация базовых сервисов
            var workspaceService = app.Services.GetRequiredService<WorkspaceService>();
            await workspaceService.InitializeAsync();

            // Прогрев LLM-сервиса
            var inferenceService = app.Services.GetRequiredService<LlamaInferenceService>();

            app.UseRouting();
            app.MapControllers();

            // Запуск Web API в фоне
            _ = app.RunAsync("http://localhost:5000");
            Console.WriteLine("[Engine] REST API запущен на http://localhost:5000");
            Console.WriteLine("[Engine] Готов к работе.\n");

            // Основной интерактивный консольный цикл
            while (true)
            {
                Console.Write("User > ");
                string? userInput = Console.ReadLine();

                if (string.IsNullOrWhiteSpace(userInput) ||
                    userInput.Equals("exit", StringComparison.OrdinalIgnoreCase))
                {
                    break;
                }

                Console.Write("Assistant > ");
                string response = await inferenceService.GenerateResponseAsync(userInput);
                Console.WriteLine(response);
                Console.WriteLine();
            }

            Console.WriteLine("Завершение работы...");
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"\n[FATAL ERROR] Программа аварийно завершила работу:");
            Console.WriteLine(ex.ToString());
            Console.ResetColor();

            Console.WriteLine("\nНажмите любую клавишу для закрытия окна...");
            Console.ReadKey();
        }
    }
}