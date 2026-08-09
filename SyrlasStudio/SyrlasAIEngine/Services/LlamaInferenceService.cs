using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using LLama;
using LLama.Common;

namespace SyrlasAIEngine.Services;

public class LlamaInferenceService : IDisposable
{
    private static readonly string[] ModelCandidates =
    [
        @"X:\SyrlasStudio\SyrlasStudio\SyrlasAIEngine\Model\qwen2.5-coder-1.5b-instruct-q4_k_m.gguf",
        @"X:\SyrlasStudio\SyrlasStudio\SyrlasAIEngine\Model\qwen2.5-coder-0.5b-instruct-q4_k_m.gguf",
        @"X:\SyrlasStudio\SyrlasAIEngine\Model\Qwen2.5-1.5B-Instruct-Q4_K_L.gguf"
    ];

    private LLamaWeights? _weights;
    private StatelessExecutor? _executor;
    private bool _isInitialized;
    private readonly SemaphoreSlim _initLock = new(1, 1);

    private async Task InitializeAsync()
    {
        if (_isInitialized) return;

        await _initLock.WaitAsync();
        try
        {
            if (_isInitialized) return;

            var modelPath = Array.Find(ModelCandidates, File.Exists)
                ?? throw new FileNotFoundException("Файл модели не найден в SyrlasAIEngine\\Model.");

            await Task.Run(() =>
            {
                var parameters = new ModelParams(modelPath)
                {
                    ContextSize = 2048,
                    GpuLayerCount = 99,
                    Threads = 6,
                    UseMemoryLock = false,
                    UseMemorymap = true
                };

                _weights = LLamaWeights.LoadFromFile(parameters);
                _executor = new StatelessExecutor(_weights, parameters);
            });

            _isInitialized = true;
        }
        finally
        {
            _initLock.Release();
        }
    }

    public async IAsyncEnumerable<string> GenerateResponseAsync(
        string prompt, 
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await InitializeAsync();

        if (_executor == null)
        {
            yield break;
        }

        var inferenceParams = new InferenceParams()
        {
            MaxTokens = 2048,
            AntiPrompts = new List<string> { "User:", "Вы:" }
        };

        await foreach (var token in _executor.InferAsync(prompt, inferenceParams, cancellationToken))
        {
            yield return token;
        }
    }

    public void Dispose()
    {
        _weights?.Dispose();
        _initLock.Dispose();
    }
}