using System;
using System.Collections.Generic;
using Vertex.Entities.Organizations;

namespace Vertex.Entities.Projects
{
    public class Project
    {
        public Guid Id { get; set; }
        public Guid OrgId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateOnly Deadline { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset UpdatedAt { get; set; }

        public Organization Organization { get; set; } = null!;
        public ICollection<ProjectMember> Members { get; set; } = new List<ProjectMember>();
        public ICollection<ProjectTask> Tasks { get; set; } = new List<ProjectTask>();
    }
}
