using System.Text.Json.Serialization;

namespace SyrlasAIEngine.Models
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum AgentRole
    {
        BA,     // Бизнес-аналитик
        SA,     // Системный аналитик
        CODER   // Кодер
    }

    public class AgentProfile
    {
        public AgentRole Role { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public double Temperature { get; set; }
        public double TopP { get; set; }
        public string BaseSystemPrompt { get; set; } = string.Empty;
    }

    public class SwitchRoleRequest
    {
        public string SessionId { get; set; } = string.Empty;
        public AgentRole Role { get; set; }
    }

    public class SaveArtifactRequest
    {
        public string SessionId { get; set; } = string.Empty;
        public string Stage { get; set; } = "BUSINESS_REQUIREMENTS"; // BUSINESS_REQUIREMENTS, TECHNICAL_SPEC, ARCHITECTURE
        public AgentRole SourceRole { get; set; }
        public string SummaryContent { get; set; } = string.Empty;
    }

    public class ChatRequest
    {
        public string SessionId { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }

    public class ChatResponse
    {
        public string MessageId { get; set; } = string.Empty;
        public AgentRole Role { get; set; }
        public string Response { get; set; } = string.Empty;
    }
}