using System;
using Vertex.Entities.Users;

namespace Vertex.Entities.Workspaces
{
    public class WorkspaceMember
    {
        public Guid Id { get; set; }
        public Guid WorkspaceId { get; set; }
        public Guid UserId { get; set; }
        public string Role { get; set; } = "member";
        public DateTimeOffset JoinedAt { get; set; }

        public Workspace Workspace { get; set; } = null!;
        public User User { get; set; } = null!;
    }
}
