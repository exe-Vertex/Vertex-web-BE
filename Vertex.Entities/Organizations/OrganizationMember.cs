using System;

namespace Vertex.Entities.Organizations
{
    public class OrganizationMember
    {
        public Guid Id { get; set; }
        public Guid OrgId { get; set; }
        public Guid UserId { get; set; }
        public string Role { get; set; } = "member"; // owner | admin | lecturer | member
        public DateTimeOffset JoinedAt { get; set; }

        public Organization Organization { get; set; } = null!;
        public Users.User User { get; set; } = null!;
    }
}
