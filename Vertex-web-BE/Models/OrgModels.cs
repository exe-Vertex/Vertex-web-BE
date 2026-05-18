using System;

namespace Vertex_web_BE.Models
{
    // ── Requests ────────────────────────────────────────

    public class CreateOrgRequest
    {
        public string Name { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
    }

    public class InviteMemberRequest
    {
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = "member";
    }

    public class UpdateMemberRoleRequest
    {
        public string Role { get; set; } = string.Empty;
    }

    // ── Responses ───────────────────────────────────────

    public class OrgSummaryResponse
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public string Plan { get; set; } = string.Empty;
        public int MemberCount { get; set; }
        public int MaxMembers { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
    }

    public class OrgDetailResponse
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public string Plan { get; set; } = string.Empty;
        public int MaxMembers { get; set; }
        public int AiQuota { get; set; }
        public long StorageLimit { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public OrgMemberResponse[] Members { get; set; } = Array.Empty<OrgMemberResponse>();
    }

    public class OrgMemberResponse
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string AvatarUrl { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public DateTimeOffset JoinedAt { get; set; }
    }
}
