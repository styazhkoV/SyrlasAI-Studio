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
    private ChatSession? _session;

    // Путь к модели Qwen 2.5 1.5B
    private const string ModelPath = @"C:\Users\alexs\SyrlasStudio\SyrlasAIEngine\Model\Qwen2.5-1.5B-Instruct-Q4_K_L.gguf";

    public bool IsInitialized { get; private set; }

    // Конструктор больше не блокирует потоки тяжелой синхронной загрузкой[cite: 3]
    public AgentService()
    {
    }

    /// <summary>
    /// Асинхронная инициализация движка в фоновом режиме с выводом статуса в лог
    /// </summary>
    public async Task InitializeAsync(Action<string>? logCallback = null)
    {
        if (IsInitialized) return;

        logCallback?.Invoke("Проверка файла весов модели...");
        if (!File.Exists(ModelPath))
        {
            throw new FileNotFoundException($"Файл модели не найден по пути: {ModelPath}");
        }

        await Task.Run(() =>
        {
            logCallback?.Invoke("Загрузка модели и выделение памяти на GTX 1070...");

            // Тонкая настройка параметров для 8 ГБ VRAM (ContextSize = 2048, GpuLayerCount = 99)[cite: 3]
            var parameters = new ModelParams(ModelPath)
            {
                GpuLayerCount = 99, 
                ContextSize = 2048, 
                BatchSize = 512,
                UseMemoryLock = false
            };

            // Загрузка весов и контекста
            _weights = LLamaWeights.LoadFromFile(parameters);
            _context = _weights.CreateContext(parameters);
            _executor = new InteractiveExecutor(_context);

            // Инициализация ChatSession для работы с KV-Cache (Prompt Caching)
            _session = new ChatSession(_executor);

            // Задание системного роли через сессию
            _session.History.AddMessage(AuthorRole.System, "You are Syrlas AI, a helpful and precise assistant.");

            IsInitialized = true;
        });

        logCallback?.Invoke("Локальный движок ИИ успешно инициализирован и готов к работе.");
    }

    /// <summary>
    /// Потоковая генерация ответа с сохранением KV-Cache через ChatSession
    /// </summary>
    public async IAsyncEnumerable<string> GenerateResponseAsync(
        string userPrompt, 
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (!IsInitialized || _session == null)
        {
            yield return "Ошибка: Локальный движок ИИ не инициализирован.";
            yield break;
        }

        var inferenceParams = new InferenceParams
        {
            MaxTokens = 1024,
            AntiPrompts = new List<string> { "<|im_end|>", "<|endoftext|>" }
        };

        // ChatSession автоматически подставляет историю и кэширует токены предыдущих сообщений
        await foreach (var token in _session.ChatAsync(userPrompt, inferenceParams, cancellationToken))
        {
            yield return token;
        }
    }

    public void Dispose()
    {
        _session = null;
        _executor = null;
        _context?.Dispose();
        _weights?.Dispose();
    }
}