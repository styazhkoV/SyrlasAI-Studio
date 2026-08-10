using System;
using System.Collections.Generic;
using System.IO;
using LLama;
using LLama.Common;
using LLama.Sampling;

namespace Syrlas.AI.Engine.Services;

public enum AgentRole { BusinessAnalyst, SystemAnalyst, Architect, Coder }

public sealed class AgentOrchestrator : IDisposable
{
    private LLamaWeights? _model;
    private ModelParams? _modelParams;
    private LLamaContext? _context; // Сохраняем контекст как поле класса
    private bool _isInitialized = false;
    private string _lastError = string.Empty;
    private bool _disposed;

    public bool IsInitialized => _isInitialized;
    public string LastError => _lastError;

    public AgentOrchestrator(string modelPath)
    {
        InitializeModel(modelPath);
    }

    private void InitializeModel(string modelPath)
    {
        try
        {
            if (!File.Exists(modelPath))
            {
                _lastError = $"Файл модели не найден по пути: {modelPath}";
                _isInitialized = false;
                System.Diagnostics.Debug.WriteLine($"[ERROR] {_lastError}");
                return;
            }

            _modelParams = new ModelParams(modelPath)
            {
                ContextSize = 4096,
                GpuLayerCount = 33
            };

            _model = LLamaWeights.LoadFromFile(_modelParams);
            
            // Инициализируем контекст один раз при старте
            _context = _model.CreateContext(_modelParams);
            
            _isInitialized = true;
            _lastError = string.Empty;
            
            System.Diagnostics.Debug.WriteLine("[INFO] Локальный движок ИИ успешно инициализирован.");
        }
        catch (Exception ex)
        {
            _isInitialized = false;
            _lastError = ex.Message;
            System.Diagnostics.Debug.WriteLine($"[CRITICAL] Ошибка инициализации LLamaWeights: {ex}");
        }
    }

    public async IAsyncEnumerable<string> ExecuteAgentStageAsync(
        AgentRole role, 
        string userInput, 
        string previousContext)
    {
        if (!_isInitialized || _model == null || _modelParams == null || _context == null)
        {
            yield return $"Ошибка: Локальный движок ИИ не инициализирован. Причина: {_lastError}";
            yield break;
        }

        var (systemPrompt, inferenceParams) = GetAgentProfile(role, previousContext);
        
        // Переиспользуем контекст, создавая новую сессию только для инференса
        var executor = new InteractiveExecutor(_context);
        var session = new ChatSession(executor);

        session.History.AddMessage(AuthorRole.System, systemPrompt);

        await foreach (var token in session.ChatAsync(
            new ChatHistory.Message(AuthorRole.User, userInput), 
            inferenceParams))
        {
            yield return token;
        }
    }

    private (string SystemPrompt, InferenceParams Params) GetAgentProfile(AgentRole role, string previousContext)
    {
        return role switch
        {
            AgentRole.BusinessAnalyst => (
                "Ты — Senior Business Analyst. Твоя задача — извлечь бизнес-требования, выявить риски и описать User Stories.\n" +
                $"Контекст проекта:\n{previousContext}",
                new InferenceParams { AntiPrompts = ["User:"], SamplingPipeline = new DefaultSamplingPipeline { Temperature = 0.6f, TopP = 0.9f } }
            ),
            AgentRole.SystemAnalyst => (
                "Ты — Senior System Analyst. Переведи бизнес-требования в OpenAPI 3.0 контракты и схемы БД.\n" +
                $"Утвержденные бизнес-требования:\n{previousContext}",
                new InferenceParams { AntiPrompts = ["User:"], SamplingPipeline = new DefaultSamplingPipeline { Temperature = 0.2f, TopP = 0.85f } }
            ),
            AgentRole.Architect => (
                "Ты — Principal .NET Architect. Спроектируй C# интерфейсы, CQRS команды и структуру классов в .NET 9.\n" +
                $"Техническое задание:\n{previousContext}",
                new InferenceParams { AntiPrompts = ["User:"], SamplingPipeline = new DefaultSamplingPipeline { Temperature = 0.1f, TopP = 0.9f } }
            ),
            AgentRole.Coder => (
                "Ты — Senior C# Developer (.NET 9). Пиши чистый, компилируемый код без лишних пояснений. " +
                "Используй C# 12/13, pattern matching и первичные конструкторы.\n" +
                $"Архитектурный план и контракты:\n{previousContext}",
                new InferenceParams { AntiPrompts = ["User:"], SamplingPipeline = new DefaultSamplingPipeline { Temperature = 0.0f, TopP = 0.95f } }
            ),
            _ => throw new ArgumentOutOfRangeException(nameof(role))
        };
    }

    public void Dispose()
    {
        if (_disposed) return;
        
        _context?.Dispose();
        _model?.Dispose();
        _disposed = true;
    }
}