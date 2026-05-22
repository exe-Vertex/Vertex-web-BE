using System;
using System.Collections.Generic;

namespace Vertex.Entities.Projects
{
    public class Project
    {
        public Guid Id { get; set; }
        public Guid OrgId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateTime Deadline { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset UpdatedAt { get; set; }

        // Navigation
        public List<ProjectTask> Tasks { get; set; } = new();
        public List<ProjectMember> Members { get; set; } = new();
        public List<ProjectFile> Files { get; set; } = new();
    }

    public class ProjectMember
    {
        public Guid Id { get; set; }
        public Guid ProjectId { get; set; }
        public Guid UserId { get; set; }
        public string Role { get; set; } = "Member"; // Leader | Member | Guest
        public DateTimeOffset JoinedAt { get; set; }

        // Navigation
        public Project? Project { get; set; }
        public Users.User? User { get; set; }
    }

    public class ProjectTask
    {
        public Guid Id { get; set; }
        public Guid ProjectId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string Status { get; set; } = "todo";       // todo | in-progress | ready-for-review | done
        public string Priority { get; set; } = "medium";    // low | medium | high
        public Guid? AssigneeId { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int Position { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset UpdatedAt { get; set; }

        // Navigation
        public Project? Project { get; set; }
        public Users.User? Assignee { get; set; }
        public List<Subtask> Subtasks { get; set; } = new();
        public List<TaskComment> Comments { get; set; } = new();
    }
}
