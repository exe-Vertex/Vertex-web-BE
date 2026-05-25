using System;
using Vertex.Entities.Users;

namespace Vertex.Entities.Projects
{
    public class ProjectMember
    {
        public Guid Id { get; set; }
        public Guid ProjectId { get; set; }
        public Guid UserId { get; set; }
        public string Role { get; set; } = "Member"; // Leader | Member | Guest
        public DateTimeOffset JoinedAt { get; set; }

        public Project Project { get; set; } = null!;
        public User User { get; set; } = null!;
    }
}
