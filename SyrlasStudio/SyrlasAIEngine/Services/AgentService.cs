using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace SyrlasAIEngine.Services
{
    public class AgentService
    {
        private readonly LlamaInferenceService _llamaService;
        private readonly PromptFactory _promptFactory;
        private readonly RagService _ragService;

        public AgentService(
            LlamaInferenceService llamaService, 
            PromptFactory promptFactory, 
            RagService ragService)
        {
            _llamaService = llamaService;
            _promptFactory = promptFactory;
            _ragService = ragService;
        }

        public async IAsyncEnumerable<string> ExecuteTaskAsync(
            AgentRole role, 
            string userQuery, 
            bool useRagContext = false,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            var profile = _promptFactory.GetProfile(role);
            string finalUserQuery = userQuery;

            if (useRagContext)
            {
                var contextChunks = await _ragService.SearchContextAsync(userQuery, limit: 3);
                if (!string.IsNullOrWhiteSpace(contextChunks))
                {
                    finalUserQuery = $"Контекст из проекта:\n\"\"\"\n{contextChunks}\n\"\"\"\n\nЗадача: {userQuery}";
                }
            }

            string fullPrompt = _promptFactory.BuildChatPrompt(profile.SystemPrompt, finalUserQuery);

            await foreach (var token in _llamaService.GenerateStreamAsync(fullPrompt, profile.DefaultParams, cancellationToken))
            {
                yield return token;
            }
        }

        public async Task SaveArtifactAsync(string title, string content, string type)
        {
            // Заглушка сохранения сгенерированного артефакта (BPMN, API Спека, Код)
            await Task.CompletedTask;
        }
    }
}