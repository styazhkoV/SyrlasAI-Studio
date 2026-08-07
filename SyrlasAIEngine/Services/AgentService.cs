using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Data.Sqlite;
using SyrlasAIEngine.Database;
using SyrlasAIEngine.Models;

namespace SyrlasAIEngine.Services
{
    public class AgentService
    {
        private readonly DatabaseInitializer _dbInit;
        private readonly PromptFactory _promptFactory;

        public AgentService(DatabaseInitializer dbInit, PromptFactory promptFactory)
        {
            _dbInit = dbInit;
            _promptFactory = promptFactory;
        }

        private SqliteConnection GetConnection() => new SqliteConnection(_dbInit.GetConnectionString());

        // Получение или автоматическое создание активной сессии
        public async Task<string> EnsureSessionAsync(string? sessionId = null)
        {
            using var conn = GetConnection();
            if (!string.IsNullOrEmpty(sessionId))
            {
                var existing = await conn.QueryFirstOrDefaultAsync<string>(
                    "SELECT id FROM sessions WHERE id = @id", new { id = sessionId });
                if (existing != null) return existing;
            }

            string newId = Guid.NewGuid().ToString();
            await conn.ExecuteAsync(@"
                INSERT INTO sessions (id, title, active_agent_role)
                VALUES (@id, @title, @role)",
                new { id = newId, title = "Новый проект", role = AgentRole.BA.ToString() });

            return newId;
        }

        // Получение текущей роли сессии
        public async Task<AgentRole> GetActiveRoleAsync(string sessionId)
        {
            using var conn = GetConnection();
            var roleStr = await conn.QueryFirstOrDefaultAsync<string>(
                "SELECT active_agent_role FROM sessions WHERE id = @id", new { id = sessionId });

            return Enum.TryParse<AgentRole>(roleStr, out var role) ? role : AgentRole.BA;
        }

        // Переключение роли (POST /api/agent/switch)
        public async Task SwitchRoleAsync(string sessionId, AgentRole newRole)
        {
            using var conn = GetConnection();
            await conn.ExecuteAsync(@"
                UPDATE sessions 
                SET active_agent_role = @role, updated_at = datetime('now')
                WHERE id = @id",
                new { id = sessionId, role = newRole.ToString() });
        }

        // Сохранение артефакта этапа (POST /api/workspace/artifact)
        public async Task SaveArtifactAsync(SaveArtifactRequest req)
        {
            using var conn = GetConnection();
            string id = Guid.NewGuid().ToString();
            
            await conn.ExecuteAsync(@"
                INSERT INTO agent_contexts (id, session_id, stage, source_agent_role, summary_content, is_active)
                VALUES (@id, @sessionId, @stage, @role, @content, 1)",
                new {
                    id,
                    sessionId = req.SessionId,
                    stage = req.Stage,
                    role = req.SourceRole.ToString(),
                    content = req.SummaryContent
                });
        }

        // Обработка сообщения от пользователя
        public async Task<ChatResponse> ProcessChatAsync(ChatRequest req)
        {
            string sessionId = await EnsureSessionAsync(req.SessionId);
            AgentRole currentRole = await GetActiveRoleAsync(sessionId);
            var profile = _promptFactory.GetProfile(currentRole);

            using var conn = GetConnection();

            // 1. Сохраняем сообщение пользователя
            string userMsgId = Guid.NewGuid().ToString();
            await conn.ExecuteAsync(@"
                INSERT INTO messages (id, session_id, sender, agent_role, content)
                VALUES (@id, @sessionId, 'user', @role, @content)",
                new { id = userMsgId, sessionId, role = currentRole.ToString(), content = req.Message });

            // 2. Читаем активные контексты для этой сессии
            var activeContexts = await conn.QueryAsync<string>(@"
                SELECT summary_content FROM agent_contexts 
                WHERE session_id = @sessionId AND is_active = 1",
                new { sessionId });

            // 3. Формируем финальный промпт через PromptFactory
            string systemPrompt = _promptFactory.BuildSystemPrompt(currentRole, activeContexts);

            // TODO: На следующем шаге тут будет вызов LLamaSharp (Inference Engine)
            // Пока делаем заглушку-ответ
            string mockReply = $"[{profile.Name} (Temp: {profile.Temperature})]: Принял ваш запрос \"{req.Message}\". Контекстов загружено: {System.Linq.Enumerable.Count(activeContexts)}.";

            // 4. Сохраняем ответ ассистента
            string assistantMsgId = Guid.NewGuid().ToString();
            await conn.ExecuteAsync(@"
                INSERT INTO messages (id, session_id, sender, agent_role, content)
                VALUES (@id, @sessionId, 'assistant', @role, @content)",
                new { id = assistantMsgId, sessionId, role = currentRole.ToString(), content = mockReply });

            return new ChatResponse
            {
                MessageId = assistantMsgId,
                Role = currentRole,
                Response = mockReply
            };
        }
    }
}