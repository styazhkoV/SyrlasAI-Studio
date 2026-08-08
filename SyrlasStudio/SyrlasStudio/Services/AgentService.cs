using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using SyrlasAIEngine.Services;

namespace SyrlasStudio.Services;

public class AgentService
{
    private readonly LlamaInferenceService _inferenceService;

    public AgentService(LlamaInferenceService inferenceService)
    {
        _inferenceService = inferenceService;
    }

    public async IAsyncEnumerable<string> GenerateResponseAsync(
        string prompt, 
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await foreach (var token in _inferenceService.GenerateResponseAsync(prompt, cancellationToken))
        {
            yield return token;
        }
    }
}