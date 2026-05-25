using System;
using System.Collections.Generic;
using Vertex.Entities.Organizations;
using Vertex.Entities.Users;

namespace Vertex.Entities.Workspaces
{
    public class Workspace
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public Guid OwnerId { get; set; }
        public Guid? OrgId { get; set; }
        public DateTimeOffset CreatedAt { get; set; }

        public User Owner { get; set; } = null!;
        public Organization? Organization { get; set; }
        public ICollection<WorkspaceMember> Members { get; set; } = new List<WorkspaceMember>();
    }
}
