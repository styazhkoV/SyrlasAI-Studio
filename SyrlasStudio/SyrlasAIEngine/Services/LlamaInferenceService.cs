using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using LLama;
using LLama.Common;
using LLama.Native;
using LLama.Sampling;

namespace SyrlasAIEngine.Services;

public sealed class LlamaInferenceService : IDisposable
{
    private readonly LLamaWeights _weights;
    private readonly LLamaContext _context;
    private readonly InteractiveExecutor _executor;
    private readonly ChatSession _chatSession;
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    public LlamaInferenceService()
    {
        string modelPath = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory,
            "Model",
            "Qwen2.5-1.5B-Instruct-Q4_K_L.gguf");

        if (!File.Exists(modelPath))
        {
            throw new FileNotFoundException($"Модель не найдена по пути: {modelPath}");
        }

        var modelParams = new ModelParams(modelPath)
        {
            GpuLayerCount = 99,
            ContextSize = 4096,
            Threads = 6, // 6 физических ядер Xeon X5660
            UseMemoryLock = true,
            UseMemorymap = true,
            TypeK = GGMLType.GGML_TYPE_Q8_0,
            TypeV = GGMLType.GGML_TYPE_Q8_0,
            FlashAttention = true,
            NoKqvOffload = false,
            BatchSize = 512
        };

        Console.WriteLine("[Engine] Загрузка модели в VRAM...");
        _weights = LLamaWeights.LoadFromFile(modelParams);
        _context = _weights.CreateContext(modelParams);
        _executor = new InteractiveExecutor(_context);

        var chatHistory = new ChatHistory();
        chatHistory.AddMessage(
            AuthorRole.System,
            "You are Syrlas AI Assistant—an expert, concise, and professional coding assistant.");

        _chatSession = new ChatSession(_executor, chatHistory);
    }

    public async Task<string> GenerateResponseAsync(string userInput, CancellationToken cancellationToken = default)
    {
        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            var inferenceParams = GetInferenceParams();
            var sb = new StringBuilder();

            await foreach (var token in _chatSession.ChatAsync(
                new ChatHistory.Message(AuthorRole.User, userInput),
                inferenceParams))
            {
                sb.Append(token);
            }

            return sb.ToString();
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public IAsyncEnumerable<string> GenerateStreamAsync(
        string userInput,
        CancellationToken cancellationToken = default) =>
        GenerateStreamAsync(userInput, GetInferenceParams(), cancellationToken);

    public async IAsyncEnumerable<string> GenerateStreamAsync(
        string userInput,
        InferenceParams inferenceParams,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            await foreach (var token in _chatSession.ChatAsync(
                new ChatHistory.Message(AuthorRole.User, userInput),
                inferenceParams))
            {
                yield return token;
            }
        }
        finally
        {
            _semaphore.Release();
        }
    }

    private static InferenceParams GetInferenceParams() => new()
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

    public void Dispose()
    {
        _semaphore.Dispose();
        _context.Dispose();
        _weights.Dispose();
    }
}