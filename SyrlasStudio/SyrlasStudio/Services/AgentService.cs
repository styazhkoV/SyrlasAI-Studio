using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using LLama;
using LLama.Common;

namespace SyrlasStudio.Services;

public class AgentService : IDisposable
{
    private LLamaWeights? _weights;
    private LLamaContext? _context;
    private InteractiveExecutor? _executor;

    // Путь к вашей модели Qwen 2.5 1.5B
    private const string ModelPath = @"C:\Users\alexs\SyrlasStudio\SyrlasAIEngine\Model\Qwen2.5-1.5B-Instruct-Q4_K_L.gguf";

    public bool IsInitialized { get; private set; }

    public AgentService()
    {
        InitializeEngine();
    }

    private void InitializeEngine()
    {
        if (!File.Exists(ModelPath))
        {
            throw new FileNotFoundException($"Файл модели не найден по пути: {ModelPath}");
        }

        // 1. Параметры загрузки модели с полным переносом на GTX 1070 VRAM
        var parameters = new ModelParams(ModelPath)
        {
            GpuLayerCount = 99, // Загружает все слои в видеопамять (минуя слабый CPU)
            ContextSize = 2048, 
            BatchSize = 512,
            UseMemoryLock = false
        };

        // 2. Загрузка весов модели
        _weights = LLamaWeights.LoadFromFile(parameters);

        // 3. Создание контекста инференса
        _context = _weights.CreateContext(parameters);

        // 4. Инициализация исполнителя диалога
        _executor = new InteractiveExecutor(_context);

        IsInitialized = true;
    }

    /// <summary>
    /// Потоковая генерация ответа токен за токеном
    /// </summary>
    public async IAsyncEnumerable<string> GenerateResponseAsync(
        string userPrompt, 
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (!IsInitialized || _executor == null)
        {
            yield return "Ошибка: Локальный движок ИИ не инициализирован.";
            yield break;
        }

        // Системный промпт для правильного формата Qwen
        string formattedPrompt = $"<|im_start|>system\nYou are Syrlas AI, a helpful and precise assistant.<|im_end|>\n<|im_start|>user\n{userPrompt}<|im_end|>\n<|im_start|>assistant\n";

        // Актуальные параметры инференса для LLamaSharp v0.27.0
        var inferenceParams = new InferenceParams
        {
            MaxTokens = 1024,
            AntiPrompts = new List<string> { "<|im_end|>", "<|endoftext|>" }
        };

        // Запуск генерации через GPU
        await foreach (var token in _executor.InferAsync(formattedPrompt, inferenceParams, cancellationToken))
        {
            yield return token;
        }
    }

    public void Dispose()
    {
        _context?.Dispose();
        _weights?.Dispose();
    }
}