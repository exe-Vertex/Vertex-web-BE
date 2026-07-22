using System;
using System.Collections.Generic;

namespace Vertex.Services.Models
{
    // ── Inputs ──────────────────────────────────────────

    public record CreateOrgInput(string Name, string Slug);

    public record InviteMemberInput(string Email, string Role);

    public record UpdateMemberRoleInput(string Role);

    // ── Outputs ─────────────────────────────────────────

    public record OrgSummary(
        Guid Id,
        string Name,
        string Slug,
        string Plan,
        int MemberCount,
        int MaxMembers,
        DateTimeOffset CreatedAt
    );

    public record OrgDetail(
        Guid Id,
        string Name,
        string Slug,
        string Plan,
        int MaxMembers,
        int AiQuota,
        int AiUsed,
        DateTimeOffset AiQuotaPeriodStart,
        long StorageLimit,
        DateTimeOffset CreatedAt,
        List<OrgMemberDto> Members
    );

    public record OrgMemberDto(
        Guid Id,
        Guid UserId,
        string Name,
        string Email,
        string AvatarUrl,
        string Role,
        DateTimeOffset JoinedAt
    );
}
