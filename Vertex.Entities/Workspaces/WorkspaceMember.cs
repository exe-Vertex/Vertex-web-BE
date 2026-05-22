using System;

namespace Vertex.Entities.Workspaces
{
    public class WorkspaceMember
    {
        public Guid Id { get; set; }
        public Guid WorkspaceId { get; set; }
        public Guid UserId { get; set; }
        public string Role { get; set; } = "member";
        public DateTimeOffset JoinedAt { get; set; }

        // Navigation
        public Workspace Workspace { get; set; } = null!;
        public Users.User User { get; set; } = null!;
    }
}
