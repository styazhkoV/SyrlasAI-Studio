using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using LLama;
using LLama.Common;
using LLama.Native;
using LLama.Sampling;

namespace SyrlasStudio.Services;

public sealed class AgentService : IDisposable
{
    private LLamaWeights? _weights;
    private LLamaContext? _context;
    private InteractiveExecutor? _executor;
    private ChatSession? _session;
    private readonly SemaphoreSlim _initLock = new(1, 1);
    private readonly SemaphoreSlim _inferenceLock = new(1, 1);
    private bool _disposed;

    private static readonly string[] ModelCandidates =
    [
        @"X:\SyrlasStudio\SyrlasAIEngine\Model\Qwen2.5-1.5B-Instruct-Q4_K_L.gguf",
        @"X:\SyrlasStudio\SyrlasStudio\SyrlasAIEngine\Model\qwen2.5-coder-1.5b-instruct-q4_k_m.gguf",
        @"X:\SyrlasStudio\SyrlasStudio\SyrlasAIEngine\Model\qwen2.5-coder-0.5b-instruct-q4_k_m.gguf"
    ];

    public bool IsInitialized { get; private set; }
    public string? LoadedModelPath { get; private set; }

    public async Task InitializeAsync(Action<string>? logCallback = null)
    {
        if (IsInitialized) return;

        await _initLock.WaitAsync().ConfigureAwait(false);
        try
        {
            if (IsInitialized) return;

            var modelPath = ResolveModelPath();
            if (modelPath is null)
            {
                throw new FileNotFoundException(
                    "Файл модели (.gguf) не найден. Проверьте каталог SyrlasAIEngine\\Model.");
            }

            LoadedModelPath = modelPath;
            logCallback?.Invoke($"Найдена модель: {Path.GetFileName(modelPath)}");

            await Task.Run(() =>
            {
                logCallback?.Invoke("Загрузка модели в память (CPU/GPU)...");

                var parameters = new ModelParams(modelPath)
                {
                    GpuLayerCount = 99,
                    ContextSize = 2048,
                    Threads = 6,
                    UseMemoryLock = false,
                    UseMemorymap = true,
                    TypeK = GGMLType.GGML_TYPE_F16,
                    TypeV = GGMLType.GGML_TYPE_F16,
                    FlashAttention = false,
                    NoKqvOffload = false,
                    BatchSize = 512
                };

                _weights = LLamaWeights.LoadFromFile(parameters);
                _context = _weights.CreateContext(parameters);
                _executor = new InteractiveExecutor(_context);
                _session = new ChatSession(_executor);

                _session.History.AddMessage(
                    AuthorRole.System,
                    "You are Syrlas AI Assistant — an expert, concise and professional coding assistant.");

                IsInitialized = true;
            }).ConfigureAwait(false);

            logCallback?.Invoke($"Локальный движок ИИ ({Path.GetFileName(modelPath)}) успешно запущен!");
        }
        finally
        {
            _initLock.Release();
        }
    }

    private static string? ResolveModelPath()
    {
        // 1. Проверка прямых путей
        foreach (var candidate in ModelCandidates)
        {
            if (File.Exists(candidate))
                return candidate;
        }

        // 2. Динамический рекурсивный поиск любого .gguf файла
        string[] searchDirectories =
        [
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Model"),
            @"X:\SyrlasStudio\SyrlasAIEngine\Model",
            @"X:\SyrlasStudio\SyrlasStudio\SyrlasAIEngine\Model",
            @"X:\SyrlasStudio"
        ];

        foreach (var dir in searchDirectories)
        {
            if (Directory.Exists(dir))
            {
                var ggufFiles = Directory.GetFiles(dir, "*.gguf", SearchOption.AllDirectories);
                if (ggufFiles.Length > 0)
                    return ggufFiles[0];
            }
        }

        return null;
    }

    public async IAsyncEnumerable<string> GenerateResponseAsync(
        string userPrompt,
        float temperature = 0.7f,
        float topP = 0.9f,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (!IsInitialized || _session == null)
        {
            yield return "Ошибка: Локальный движок ИИ не инициализирован.";
            yield break;
        }

        await _inferenceLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            while (_session.History.Messages.Count > 0 &&
                   _session.History.Messages[^1].AuthorRole == AuthorRole.User)
            {
                _session.History.Messages.RemoveAt(_session.History.Messages.Count - 1);
            }

            var safeTemperature = Math.Max(0.1f, temperature);

            var inferenceParams = new InferenceParams
            {
                MaxTokens = 1024,
                AntiPrompts = new List<string> { "<|im_end|>", "<|im_start|>" },
                SamplingPipeline = new DefaultSamplingPipeline
                {
                    Temperature = safeTemperature,
                    TopP = topP,
                    TopK = 40,
                    RepeatPenalty = 1.15f
                }
            };

            var chatMessage = new ChatHistory.Message(AuthorRole.User, userPrompt);

            await foreach (var token in _session.ChatAsync(chatMessage, inferenceParams, cancellationToken))
            {
                yield return token;
            }
        }
        finally
        {
            _inferenceLock.Release();
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        
        _initLock.Dispose();
        _inferenceLock.Dispose();
        
        _session = null;
        _executor = null;
        
        _context?.Dispose();
        _weights?.Dispose();
        
        _disposed = true;
    }
}