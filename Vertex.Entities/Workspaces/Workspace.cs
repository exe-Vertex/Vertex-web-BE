using System;
using System.Collections.Generic;

namespace Vertex.Entities.Workspaces
{
    public class Workspace
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public Guid OwnerId { get; set; }
        public Guid? OrgId { get; set; } // nullable for personal workspaces
        public DateTimeOffset CreatedAt { get; set; }

        // Navigation
        public Users.User Owner { get; set; } = null!;
        public Organizations.Organization? Organization { get; set; }
        public ICollection<WorkspaceMember> Members { get; set; } = new List<WorkspaceMember>();
    }
}
