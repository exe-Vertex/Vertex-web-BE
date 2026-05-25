using System;
using Vertex.Entities.Users;

namespace Vertex.Entities.Projects
{
    public class TaskComment
    {
        public Guid Id { get; set; }
        public Guid TaskId { get; set; }
        public Guid UserId { get; set; }
        public string Content { get; set; } = string.Empty;
        public DateTimeOffset CreatedAt { get; set; }

        public ProjectTask Task { get; set; } = null!;
        public User User { get; set; } = null!;
    }
}
