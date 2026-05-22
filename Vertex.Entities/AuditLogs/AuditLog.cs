using System;

namespace Vertex.Entities.AuditLogs
{
    public class AuditLog
    {
        public Guid Id { get; set; }
        public Guid AdminId { get; set; }
        public string Action { get; set; } = string.Empty; // ban_user | unban_user | change_quota | change_price
        public Guid? TargetUserId { get; set; }
        public string? Detail { get; set; }
        public DateTimeOffset CreatedAt { get; set; }

        // Navigation
        public Users.User Admin { get; set; } = null!;
        public Users.User? TargetUser { get; set; }
    }
}
