using System;
using System.Collections.Generic;

namespace Vertex.Entities.Organizations
{
    public class Organization
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public string Plan { get; set; } = "free";
        public int MaxMembers { get; set; } = 5;
        public int MaxProjects { get; set; } = 3;
        public int AiQuota { get; set; } = 20;
        public int AiUsed { get; set; } = 0;
        public DateTimeOffset AiQuotaPeriodStart { get; set; }
        public long StorageLimit { get; set; } = 1073741824; // 1 GB
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset UpdatedAt { get; set; }

        public ICollection<OrganizationMember> Members { get; set; } = new List<OrganizationMember>();
    }
}
