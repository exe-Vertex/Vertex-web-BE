using System;
using System.Collections.Generic;
using Vertex.Entities.Users;

namespace Vertex.Entities.Projects
{
    public class ProjectTask
    {
        public Guid Id { get; set; }
        public Guid ProjectId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string Status { get; set; } = "todo"; // todo | in-progress | ready-for-review | done
        public string Priority { get; set; } = "medium"; // low | medium | high
        public Guid? AssigneeId { get; set; }
        public DateOnly StartDate { get; set; }
        public DateOnly EndDate { get; set; }
        public int Position { get; set; } = 0;
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset UpdatedAt { get; set; }

        public Project Project { get; set; } = null!;
        public User? Assignee { get; set; }
        public ICollection<TaskComment> Comments { get; set; } = new List<TaskComment>();
    }
}
