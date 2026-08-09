using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using LLama;
using LLama.Common;
using LLama.Native;
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

        public async Task LoadModelAsync(
            string modelPath, 
            int contextSize = 2048, 
            int gpuLayerCount = 99)          // ← теперь по умолчанию все слои
        {
            await _lock.WaitAsync();
            try
            {
                UnloadModelInternal();

                var parameters = new ModelParams(modelPath)
                {
                    // GPU
                    GpuLayerCount = gpuLayerCount,

                    // Контекст
                    ContextSize = (uint)contextSize,

                    // CPU (Xeon X5660 — 6 физических ядер)
                    Threads = 6,

                    // Память
                    UseMemoryLock = true,
                    UseMemorymap = true,

                    // KV Cache + Flash Attention (важно для скорости)
                    TypeK = GGMLType.GGML_TYPE_Q8_0,
                    TypeV = GGMLType.GGML_TYPE_Q8_0,
                    FlashAttention = false,          // на GTX 1070 часто быстрее false
                    NoKqvOffload = false,

                    // Батч
                    BatchSize = 512
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