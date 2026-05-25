using System;

namespace Vertex.Entities.Organizations
{
    public class Invitation
    {
        public Guid Id { get; set; }
        public string Email { get; set; } = string.Empty;
        
        // "Project" or "Organization"
        public string TargetType { get; set; } = string.Empty;
        
        // ProjectId or OrgId
        public Guid TargetId { get; set; }
        
        // "Member", "Leader", "Admin"
        public string Role { get; set; } = string.Empty;
        
        public string Token { get; set; } = string.Empty;
        
        // "Pending", "Accepted", "Expired", "Revoked"
        public string Status { get; set; } = "Pending";
        
        public Guid CreatedBy { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset ExpiredAt { get; set; }
    }
}
