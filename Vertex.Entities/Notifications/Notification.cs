using System;
using Vertex.Entities.Users;

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

        public User User { get; set; } = null!;
    }
}
