using System;

namespace Vertex.Entities.Notifications
{
    public class Notification
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public string Type { get; set; } = "info"; // info | warning | error | invite
        public string Message { get; set; } = string.Empty;
        public bool IsRead { get; set; } = false;
        public DateTimeOffset CreatedAt { get; set; }

        // Navigation
        public Users.User User { get; set; } = null!;
    }
}
