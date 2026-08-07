using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using LLama;
using LLama.Common;
using LLama.Sampling;

namespace SyrlasAIEngine.Services
{
    public class LlamaInferenceService : IDisposable
    {
        private LLamaWeights? _weights;
        private LLamaContext? _context;
        private InteractiveExecutor? _executor;
        private readonly SemaphoreSlim _lock = new(1, 1);
        private bool _isLoaded = false;

        public bool IsLoaded => _isLoaded;

        public async Task LoadModelAsync(string modelPath, int contextSize = 4096, int gpuLayerCount = 20)
        {
            await _lock.WaitAsync();
            try
            {
                UnloadModelInternal();

                var parameters = new ModelParams(modelPath)
                {
                    ContextSize = (uint)contextSize,
                    GpuLayerCount = gpuLayerCount,
                    Threads = Math.Max(1, Environment.ProcessorCount - 2)
                };

                _weights = await Task.Run(() => LLamaWeights.LoadFromFile(parameters));
                _context = _weights.CreateContext(parameters);
                _executor = new InteractiveExecutor(_context);
                _isLoaded = true;
            }
            finally
            {
                _lock.Release();
            }
        }

        public async IAsyncEnumerable<string> GenerateStreamAsync(
            string prompt, 
            InferenceParams? inferenceParams = null,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            if (!_isLoaded || _executor == null)
                throw new InvalidOperationException("Модель GGUF не загружена в память.");

            await _lock.WaitAsync(cancellationToken);
            try
            {
                inferenceParams ??= new InferenceParams
                {
                    SamplingPipeline = new DefaultSamplingPipeline { Temperature = 0.2f, TopP = 0.95f },
                    MaxTokens = 2048,
                    AntiPrompts = new List<string> { "<|im_end|>", "User:" }
                };

                await foreach (var token in _executor.InferAsync(prompt, inferenceParams, cancellationToken))
                {
                    yield return token;
                }
            }
            finally
            {
                _lock.Release();
            }
        }

        public void UnloadModel()
        {
            _lock.Wait();
            try
            {
                UnloadModelInternal();
            }
            finally
            {
                _lock.Release();
            }
        }

        private void UnloadModelInternal()
        {
            _context?.Dispose();
            _weights?.Dispose();
            _context = null;
            _weights = null;
            _executor = null;
            _isLoaded = false;
        }

        public void Dispose()
        {
            UnloadModelInternal();
            _lock.Dispose();
        }
    }
}