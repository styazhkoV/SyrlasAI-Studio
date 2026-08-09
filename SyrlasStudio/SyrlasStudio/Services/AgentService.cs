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

public class AgentService : IDisposable
{
    private LLamaWeights? _weights;
    private LLamaContext? _context;
    private InteractiveExecutor? _executor;
    private ChatSession? _session;
    private readonly SemaphoreSlim _initLock = new(1, 1);

    // Модель 1.5B (совместима с текущим бэкендом LLamaSharp 0.27)
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
                    "Файл модели не найден. Проверьте каталог SyrlasAIEngine\\Model.");
            }

            LoadedModelPath = modelPath;
            logCallback?.Invoke($"Проверка файла весов модели: {Path.GetFileName(modelPath)}");

            await Task.Run(() =>
            {
                logCallback?.Invoke("Загрузка модели (CPU/GPU, безопасный KV-cache)...");

                // ВАЖНО: квантизация V-cache (Q8_0) требует FlashAttention.
                // При FlashAttention=false + TypeV=Q8_0 llama.cpp падает с 0xC0000005
                // без managed-исключения — окно мгновенно закрывается.
                var parameters = new ModelParams(modelPath)
                {
                    // Все слои на GPU (лишние значения обрезаются до n_layer)
                    GpuLayerCount = 99,

                    ContextSize = 2048,

                    // Xeon X5660 — 6 физических ядер
                    Threads = 6,

                    // mlock без SeLockMemoryPrivilege на Windows часто падает
                    UseMemoryLock = false,
                    UseMemorymap = true,

                    // F16 KV — стабильно на GTX 1070 без FlashAttention
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

            logCallback?.Invoke(
                $"Локальный движок ИИ ({Path.GetFileName(modelPath)}) успешно инициализирован.");
        }
        finally
        {
            _initLock.Release();
        }
    }

    private static string? ResolveModelPath()
    {
        foreach (var candidate in ModelCandidates)
        {
            if (File.Exists(candidate))
                return candidate;
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

        while (_session.History.Messages.Count > 0 &&
               _session.History.Messages[^1].AuthorRole == AuthorRole.User)
        {
            _session.History.Messages.RemoveAt(_session.History.Messages.Count - 1);
        }

        var safeTemperature = Math.Max(0.1f, temperature);

        var inferenceParams = new InferenceParams
        {
            MaxTokens = 1024,
            AntiPrompts = new List<string>
            {
                "<|im_end|>",
                "<|im_start|>"
            },
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

    public void Dispose()
    {
        _session = null;
        _executor = null;
        _context?.Dispose();
        _weights?.Dispose();
        _initLock.Dispose();
    }
}
