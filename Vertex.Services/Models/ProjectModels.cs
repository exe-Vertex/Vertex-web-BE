using System;
using System.Collections.Generic;

namespace Vertex.Services.Models
{
    // ── Inputs ──────────────────────────────────────────

    public record CreateProjectInput(string Name, string? Description, DateTime Deadline);

    public record UpdateProjectInput(string? Name, string? Description, DateTime? Deadline);

    public record CreateTaskInput(
        string Title,
        string? Description,
        string Status,
        string Priority,
        Guid? AssigneeId,
        DateTime StartDate,
        DateTime EndDate
    );

    public record UpdateTaskInput(
        string? Title,
        string? Description,
        string? Status,
        string? Priority,
        Guid? AssigneeId,
        DateTime? StartDate,
        DateTime? EndDate,
        int? Position
    );

    // ── Outputs ─────────────────────────────────────────

    public record ProjectMemberDto(
        Guid Id,
        Guid UserId,
        string Name,
        string Email,
        string AvatarUrl,
        string Role
    );

    public record TaskDto(
        Guid Id,
        string Title,
        string? Description,
        string Status,
        string Priority,
        ProjectMemberDto? Assignee,
        DateTime StartDate,
        DateTime EndDate,
        int Position,
        DateTimeOffset CreatedAt
    );

    public record ProjectSummaryDto(
        Guid Id,
        string Name,
        string? Description,
        DateTime Deadline,
        int TaskCount,
        int MemberCount,
        int Progress,
        DateTimeOffset CreatedAt
    );

    public record ProjectDetailDto(
        Guid Id,
        string Name,
        string? Description,
        DateTime Deadline,
        int Progress,
        DateTimeOffset CreatedAt,
        List<TaskDto> Tasks,
        List<ProjectMemberDto> Members
    );
}
