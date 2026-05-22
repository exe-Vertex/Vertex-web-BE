using System;

namespace Vertex.Entities.Projects
{
    public class Subtask
    {
        public Guid Id { get; set; }
        public Guid TaskId { get; set; }
        public string Title { get; set; } = string.Empty;
        public bool IsCompleted { get; set; } = false;
        public int Position { get; set; } = 0;

        // Navigation
        public ProjectTask Task { get; set; } = null!;
    }
}
