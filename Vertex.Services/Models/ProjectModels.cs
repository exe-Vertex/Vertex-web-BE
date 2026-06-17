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
        DateTime EndDate,
        string? SubmissionLink
    );

    public record UpdateTaskInput(
        string? Title,
        string? Description,
        string? Status,
        string? Priority,
        Guid? AssigneeId,
        DateTime? StartDate,
        DateTime? EndDate,
        int? Position,
        string? SubmissionLink
    );

    public record AddProjectMemberInput(string EmailOrUserId, string Role, string? ProjectSkills = null);

    public record UpdateProjectMemberInput(string Role, string? ProjectSkills = null);

    // ── Outputs ─────────────────────────────────────────

    public record ProjectMemberDto(
        Guid Id,
        Guid UserId,
        string Name,
        string Email,
        string AvatarUrl,
        string Role,
        string? ProjectSkills = null
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
        string? SubmissionLink,
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

    public record ProjectFileDto(
        Guid Id,
        Guid ProjectId,
        string FileName,
        string FileUrl,
        long Size,
        string? MimeType,
        Guid UploadedById,
        string UploadedBy,
        DateTimeOffset UploadedAt,
        string SizeLabel
    );

    public record CreateProjectLinkInput(string Url, string? Title);

    public record ProjectLinkDto(
        Guid Id,
        Guid ProjectId,
        string Url,
        string Title,
        Guid? UploadedById,
        string UploadedBy,
        DateTimeOffset UploadedAt
    );

    public record CreateTaskLinkInput(string Url, string? Title);

    public record TaskAttachmentDto(
        Guid Id,
        Guid TaskId,
        string Type,
        string? Url,
        string? Title,
        long? Size,
        string? SizeLabel,
        string? MimeType,
        Guid? UploadedById,
        string UploadedBy,
        DateTimeOffset UploadedAt
    );

    // ── Subtasks ────────────────────────────────────────

    public record SubtaskDto(Guid Id, Guid TaskId, string Title, bool IsCompleted, int Position);

    public record CreateSubtaskInput(string Title);

    public record UpdateSubtaskInput(string? Title, bool? IsCompleted, int? Position);

    // ── Task Comments ──────────────────────────────────
    
    public record ProjectTaskCommentDto(Guid Id, Guid TaskId, Guid UserId, string UserName, string UserAvatar, string Content, DateTimeOffset CreatedAt);

    public record CreateTaskCommentInput(string Content);
}

