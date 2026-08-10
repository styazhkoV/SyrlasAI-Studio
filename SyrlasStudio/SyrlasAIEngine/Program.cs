using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using LLama;
using LLama.Common;
using LLama.Native;
using LLama.Sampling;
using SyrlasAIEngine.Services;

namespace SyrlasAIEngine;

public class Program
{
    public static async Task Main(string[] args)
    {
        try
        {
            Console.WriteLine("=== Syrlas AI Local Engine ===");
            Console.WriteLine("Model : Qwen2.5-1.5B-Instruct-Q4_K_L");
            Console.WriteLine("GPU   : NVIDIA GeForce GTX 1070 (8 GB)");
            Console.WriteLine("CPU   : Intel Xeon X5660 (6C/12T)");
            Console.WriteLine();

            // 1. Инициализация сервисов
            var workspaceService = new WorkspaceService();
            await workspaceService.InitializeAsync();

            // 2. Путь к модели
            string modelPath = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "Model",
                "Qwen2.5-1.5B-Instruct-Q4_K_L.gguf");

            if (!File.Exists(modelPath))
            {
                Console.WriteLine($"[Error] Модель не найдена:\n{modelPath}");
                return;
            }

            // 3. Оптимизированные параметры под GTX 1070 + Xeon X5660
            var modelParams = new ModelParams(modelPath)
            {
                // GPU
                GpuLayerCount = 99,

                // Контекст
                ContextSize = 4096,

                // CPU (физические ядра Xeon X5660)
                Threads = 6,

                // Память
                UseMemoryLock = true,
                UseMemorymap = true,

                // KV Cache + Flash Attention
                TypeK = GGMLType.GGML_TYPE_Q8_0,
                TypeV = GGMLType.GGML_TYPE_Q8_0,
                FlashAttention = true,
                NoKqvOffload = false,

                // Батч
                BatchSize = 512
            };

            Console.WriteLine("[Engine] Загрузка модели в VRAM...");
            using var weights = LLamaWeights.LoadFromFile(modelParams);
            using var context = weights.CreateContext(modelParams);

            var executor = new InteractiveExecutor(context);

            // Системный промпт
            var chatHistory = new ChatHistory();
            chatHistory.AddMessage(AuthorRole.System,
                "You are Syrlas AI Assistant — an expert, concise and professional coding assistant.");

            var chatSession = new ChatSession(executor, chatHistory);

            // Параметры генерации
            var inferenceParams = new InferenceParams
            {
                MaxTokens = 2048,
                AntiPrompts = new List<string> { "<|im_end|>", "<|im_start|>" },
                SamplingPipeline = new DefaultSamplingPipeline
                {
                    Temperature = 0.7f,
                    TopP = 0.9f,
                    TopK = 40,
                    RepeatPenalty = 1.15f
                }
            };

            Console.WriteLine("[Engine] Готов к работе.\n");

            // Основной цикл диалога
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

                await foreach (var token in chatSession.ChatAsync(
                    new ChatHistory.Message(AuthorRole.User, userInput),
                    inferenceParams))
                {
                    Console.Write(token);
                }

                Console.WriteLine("\n");
            }

            Console.WriteLine("Завершение работы...");
        }
        catch (Exception ex)
        {
            // Защита от мгновенного схлопывания: выводим ошибку и ждем нажатия клавиши
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"\n[FATAL ERROR] Программа аварийно завершила работу:");
            Console.WriteLine(ex.ToString());
            Console.ResetColor();

            Console.WriteLine("\nНажмите любую клавишу для закрытия окна...");
            Console.ReadKey();
        }
    }
}

