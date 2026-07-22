using System;
using System.Collections.Generic;

namespace Vertex.Entities.Users
{
    public class User
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public string AuthProvider { get; set; } = "local"; // local | google | github
        public string? ExternalId { get; set; } // Google/GitHub user ID
        public string AvatarUrl { get; set; } = string.Empty;
        public string Role { get; set; } = "member"; // member | lecturer | admin (system-level)
        public string Status { get; set; } = "active";
        public string Title { get; set; } = string.Empty;
        public string Bio { get; set; } = string.Empty;
        public string Availability { get; set; } = "available";
        public int AiQuota { get; set; } = 20;
        public int AiUsed { get; set; } = 0;
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset UpdatedAt { get; set; }

        // Navigation
        public ICollection<Auth.RefreshToken> RefreshTokens { get; set; } = new List<Auth.RefreshToken>();
        public ICollection<Auth.PasswordResetToken> PasswordResetTokens { get; set; } = new List<Auth.PasswordResetToken>();
        public ICollection<Organizations.OrganizationMember> OrganizationMemberships { get; set; } = new List<Organizations.OrganizationMember>();
        public ICollection<UserSkill> Skills { get; set; } = new List<UserSkill>();
        public ICollection<Workspaces.WorkspaceMember> WorkspaceMemberships { get; set; } = new List<Workspaces.WorkspaceMember>();
        public ICollection<Notifications.Notification> Notifications { get; set; } = new List<Notifications.Notification>();
        public ICollection<AI.AiHistory> AiHistories { get; set; } = new List<AI.AiHistory>();
    }
}
