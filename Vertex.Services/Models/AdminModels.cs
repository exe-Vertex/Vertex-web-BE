using System;
using System.Collections.Generic;

namespace Vertex.Services.Models
{
    /// <summary>User DTO returned by Admin endpoints. Matches FE AdminUserEntry type.</summary>
    public record AdminUserDto(
        Guid Id,
        string Name,
        string Email,
        string Avatar,
        string Status,
        string Plan,
        DateTimeOffset CreatedAt,
        int AiQuota,
        int AiUsed
    );

    /// <summary>Paginated result wrapper for admin user listing.</summary>
    public record AdminUserListResult(
        List<AdminUserDto> Users,
        int TotalCount,
        int Page,
        int PageSize
    );

    /// <summary>Input for updating user status (ban/unban).</summary>
    public record UpdateUserStatusInput(string Status);

    /// <summary>Input for updating user AI quota.</summary>
    public record UpdateUserAiQuotaInput(int AiQuota);

    /// <summary>Audit log DTO returned to FE.</summary>
    public record AuditLogDto(
        Guid Id,
        string Admin,
        string Action,
        string? Target,
        string Detail,
        DateTimeOffset Timestamp
    );

    /// <summary>Paginated result wrapper for audit logs.</summary>
    public record AuditLogListResult(
        List<AuditLogDto> Logs,
        int TotalCount,
        int Page,
        int PageSize
    );
}
