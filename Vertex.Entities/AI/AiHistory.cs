using System;

namespace Vertex.Entities.AI
{
    public class AiHistory
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public string Prompt { get; set; } = string.Empty;
        public string? PlanSummary { get; set; }
        public string? PlanData { get; set; } // JSONB — stored as string, parse in service layer
        public int TokensUsed { get; set; } = 0;
        public DateTimeOffset CreatedAt { get; set; }

        // Navigation
        public Users.User User { get; set; } = null!;
    }
}
