using System;
using System.Collections.Generic;

namespace Vertex.Services.Models
{
    // ── Lecturer Group list item ──────────────────────────────────────
    public record LecturerGroupDto(
        Guid ProjectId,
        string ProjectName,
        string? ProjectDescription,
        string OrgName,
        DateOnly Deadline,
        int MemberCount,
        List<string> MemberInitials,
        int Progress,
        string ReviewStatus,  // on-track | at-risk | overdue
        int TasksTotal,
        int TasksApproved,
        int TasksInReview,
        int TasksInProgress,
        int TasksTodo
    );

    // ── Task DTO ─────────────────────────────────────────────────────
    public record LecturerTaskDto(
        Guid Id,
        string Title,
        string? Description,
        string Status,
        string Priority,
        string? AssigneeName,
        DateOnly StartDate,
        DateOnly EndDate
    );

    // ── Task Comment DTO ─────────────────────────────────────────────
    public record TaskCommentDto(
        Guid Id,
        Guid TaskId,
        Guid UserId,
        string AuthorName,
        string Role,   // lecturer | student
        string Content,
        DateTimeOffset CreatedAt
    );

    // ── Member contribution ──────────────────────────────────────────
    public record MemberContributionDto(
        string Name,
        int Assigned,
        int Approved,
        int InReview,
        int InProgress,
        int Todo,
        int CompletionPercent
    );

    // ── Group detail ─────────────────────────────────────────────────
    public record LecturerGroupDetailDto(
        Guid ProjectId,
        string ProjectName,
        string? ProjectDescription,
        string OrgName,
        DateOnly Deadline,
        int MemberCount,
        List<string> MemberNames,
        int Progress,
        string ReviewStatus,
        List<LecturerTaskDto> Tasks,
        List<TaskCommentDto> Comments,
        List<MemberContributionDto> Contributions
    );

    // ── Notification DTO ─────────────────────────────────────────────
    public record NotificationDto(
        Guid Id,
        string Type,
        string Message,
        bool IsRead,
        DateTimeOffset CreatedAt
    );
}
