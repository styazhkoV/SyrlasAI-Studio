using System;
using System.Collections.Generic;
using System.Text;
using SyrlasAIEngine.Models;

namespace SyrlasAIEngine.Services
{
    public class PromptFactory
    {
        private static readonly Dictionary<AgentRole, AgentProfile> Profiles = new()
        {
            [AgentRole.BA] = new AgentProfile
            {
                Role = AgentRole.BA,
                Name = "Бизнес-аналитик",
                Description = "Сбор требований, проведение интервью, выявление болей, генерация PlantUML диаграмм процессов.",
                Temperature = 0.7,
                TopP = 0.90,
                BaseSystemPrompt = @"Ты — высококлассный Бизнес-аналитик (BA). Твоя задача:
1. Задавать уточняющие вопросы пользователю для глубокого понимания бизнес-логики и целей проекта.
2. Формулировать User Stories, Acceptance Criteria и описывать бизнес-процессы.
3. При необходимости генерировать диаграммы процессов в формате PlantUML (`@startuml ... @enduml`).
4. Будь проактивен, ищи краевые случаи (edge cases) и риски проекта."
            },
            [AgentRole.SA] = new AgentProfile
            {
                Role = AgentRole.SA,
                Name = "Системный аналитик",
                Description = "Проектирование архитектуры, схем БД (PostgreSQL/SQLite), REST API контрактов и BPMN/UML спецификаций.",
                Temperature = 0.3,
                TopP = 0.85,
                BaseSystemPrompt = @"Ты — ведущий Системный аналитик (SA). Твоя задача:
1. Переводить бизнес-требования БА в строгие технические спецификации.
2. Проектировать реляционные схемы БД, эндпоинты REST API (Request/Response DTO), интеграции и структуры данных.
3. Соблюдать строгую техническую логику, исключая неяности и размытые формулировки."
            },
            [AgentRole.CODER] = new AgentProfile
            {
                Role = AgentRole.CODER,
                Name = "Senior Кодер",
                Description = "Написание чистого, промышленного кода (C# .NET, React/TypeScript) строго по техническому заданию.",
                Temperature = 0.1,
                TopP = 0.90,
                BaseSystemPrompt = @"Ты — Senior Software Engineer. Твоя задача:
1. Писать чистый, готовый к продакшену код строго на основе предоставленного Технического Задания (ТЗ).
2. Нулевая креативность в бизнес-логике: НЕ выдумывай требования, которых нет в ТЗ.
3. Соблюдай DRY, SOLID и лучшие практики проектирования."
            }
        };

        public AgentProfile GetProfile(AgentRole role) => Profiles[role];

        public IEnumerable<AgentProfile> GetAllProfiles() => Profiles.Values;

        public string BuildSystemPrompt(AgentRole role, IEnumerable<string> activeContexts)
        {
            var profile = Profiles[role];
            var sb = new StringBuilder();
            sb.AppendLine(profile.BaseSystemPrompt);

            // Инжектируем утвержденные артефакты от предыдущих агентов (например БА -> СА или СА -> Кодер)
            var contextsList = new List<string>(activeContexts);
            if (contextsList.Count > 0)
            {
                sb.AppendLine("\n=== УТВЕРЖДЕННЫЙ КОНТЕКСТ ПРОЕКТА (ТЗ / ТРЕБОВАНИЯ) ===");
                foreach (var ctx in contextsList)
                {
                    sb.AppendLine(ctx);
                    sb.AppendLine("---");
                }
                sb.AppendLine("=== КОНЕЦ КОНТЕКСТА ===\nИспользуй этот контекст как первоисточник правды.");
            }

            return sb.ToString();
        }
    }
}