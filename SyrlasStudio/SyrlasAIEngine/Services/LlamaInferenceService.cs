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
    private readonly string _modelPath = @"C:\Users\alexs\SyrlasStudio\SyrlasAIEngine\Model\qwen2.5-14b-instruct-uncensored-q5_k_m.gguf";
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

            if (!File.Exists(_modelPath))
            {
                throw new FileNotFoundException($"Файл модели не найден по пути: {_modelPath}");
            }

            await Task.Run(() =>
            {
                var parameters = new ModelParams(_modelPath)
                {
                    ContextSize = 4096,
                    GpuLayerCount = 32,
                    Threads = 4
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