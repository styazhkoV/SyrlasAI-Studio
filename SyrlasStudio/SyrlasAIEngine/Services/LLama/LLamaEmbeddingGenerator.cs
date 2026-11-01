using LLama;
using LLama.Common;

namespace SyrlasAIEngine.Services.LLama
{
    public class LLamaEmbeddingGenerator : IEmbeddingGenerator, IDisposable
    {
        private readonly LLamaWeights _weights;
        private readonly LLamaEmbedder _embedder;

        public LLamaEmbeddingGenerator(string modelPath, int contextSize = 2048)
        {
            var parameters = new ModelParams(modelPath)
            {
                ContextSize = (uint)contextSize,
                // Для эмбеддингов обязательно включаем оценку эмбеддингов (embeddings = true)
                Embeddings = true,
                GpuLayerCount = 33 // Полная разгрузка на видеокарту (например, GTX 1070)
            };

            // Загружаем веса и создаем контекст
            _weights = LLamaWeights.LoadFromFile(parameters);
            _embedder = new LLamaEmbedder(_weights, parameters);
        }

        public async Task<float[]> GenerateEmbeddingAsync(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return [];

            // Асинхронно получаем эмбеддинг текста через LLamaEmbedder
            var embeddings = await _embedder.GetEmbeddings(text);
            return embeddings.Count == 0 ? [] : embeddings[0];
        }

        public void Dispose()
        {
            _embedder?.Dispose();
            _weights?.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}