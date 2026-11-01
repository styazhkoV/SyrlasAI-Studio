using System;
using LLama.Common;
using LLama.Sampling;

namespace SyrlasAIEngine.Services
{
    public enum AgentRole
    {
        BusinessAnalyst,
        SystemAnalyst,
        Architect,
        Developer
    }

    public record AgentProfile(string SystemPrompt, InferenceParams DefaultParams);

    public class PromptFactory
    {
        public AgentProfile GetProfile(AgentRole role) => role switch
        {
            AgentRole.BusinessAnalyst => new AgentProfile(
                SystemPrompt: @"Ты — ведущий Бизнес-аналитик (Lead Business Analyst).
Твоя задача: формализовать требования, проектировать пользовательские сценарии (Use Cases), описывать бизнес-процессы в нотации BPMN 2.0, формулировать критерии приемки (Acceptance Criteria) и детализировать бизнес-логику.",
                DefaultParams: new InferenceParams
                {
                    SamplingPipeline = new DefaultSamplingPipeline { Temperature = 0.4f, TopP = 0.90f },
                    MaxTokens = 2048,
                    AntiPrompts = new[] { "<|im_end|>", "User:" }
                }),

            AgentRole.SystemAnalyst => new AgentProfile(
                SystemPrompt: @"Ты — ведущий Системный аналитик (Lead System Analyst).
Твоя задача: проектировать REST/gRPC API контракты (OpenAPI/Swagger), структуры БД (PostgreSQL/SQLite), схемы данных (JSON/DTO) и Sequence-диаграммы (PlantUML/Mermaid).",
                DefaultParams: new InferenceParams
                {
                    SamplingPipeline = new DefaultSamplingPipeline { Temperature = 0.2f, TopP = 0.95f },
                    MaxTokens = 2048,
                    AntiPrompts = new[] { "<|im_end|>", "User:" }
                }),

            AgentRole.Architect => new AgentProfile(
                SystemPrompt: @"Ты — Главный архитектор ПО (Principal Software Architect).
Твоя задача: проектировать архитектуру систем (Clean Architecture, DDD, CQRS, Event-Driven), выбирать паттерны проектирования и модели C4.",
                DefaultParams: new InferenceParams
                {
                    SamplingPipeline = new DefaultSamplingPipeline { Temperature = 0.3f, TopP = 0.90f },
                    MaxTokens = 2048,
                    AntiPrompts = new[] { "<|im_end|>", "User:" }
                }),

            AgentRole.Developer => new AgentProfile(
                SystemPrompt: @"Ты — Senior Fullstack & Engine Developer (эксперт в C, C++, C#/.NET 9).
Твоя задача: писать высокопроизводительный, безопасный к памяти, чисто отформатированный и готовый к production код.",
                DefaultParams: new InferenceParams
                {
                    SamplingPipeline = new DefaultSamplingPipeline { Temperature = 0.1f, TopP = 0.95f },
                    MaxTokens = 3072,
                    AntiPrompts = new[] { "<|im_end|>", "User:" }
                }),

            _ => throw new ArgumentOutOfRangeException(nameof(role), role, null)
        };

        public string BuildChatPrompt(string systemPrompt, string userPrompt)
        {
            return $"<|im_start|>system\n{systemPrompt}<|im_end|>\n<|im_start|>user\n{userPrompt}<|im_end|>\n<|im_start|>assistant\n";
        }
    }
}