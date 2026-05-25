using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Vertex.Repositories.Interfaces;
using Vertex.Services.Interfaces;
using Vertex.Services.Models;

namespace Vertex.Services.Services
{
    public class LecturerService : ILecturerService
    {
        private readonly IOrganizationRepository _orgRepo;
        private readonly IProjectRepository _projectRepo;
        private readonly INotificationRepository _notifRepo;

        public LecturerService(
            IOrganizationRepository orgRepo,
            IProjectRepository projectRepo,
            INotificationRepository notifRepo)
        {
            _orgRepo = orgRepo;
            _projectRepo = projectRepo;
            _notifRepo = notifRepo;
        }

        // ── Get all groups for a lecturer ─────────────────────────────
        public async Task<List<LecturerGroupDto>> GetGroupsAsync(Guid lecturerId)
        {
            // Find orgs where the lecturer is a member with role 'lecturer'
            var orgs = await _orgRepo.GetByUserIdAsync(lecturerId);
            var results = new List<LecturerGroupDto>();

            foreach (var org in orgs)
            {
                // Check the lecturer role in this org
                var member = await _orgRepo.GetMemberAsync(org.Id, lecturerId);
                if (member == null || member.Role != "lecturer")
                    continue;

                var projects = await _projectRepo.GetByOrgIdAsync(org.Id);
                foreach (var project in projects)
                {
                    var members = await _projectRepo.GetMembersByProjectIdAsync(project.Id);
                    var tasks = await _projectRepo.GetTasksByProjectIdAsync(project.Id);

                    var total = tasks.Count;
                    var approved = tasks.Count(t => t.Status == "done" || t.Status == "approved");
                    var inReview = tasks.Count(t => t.Status == "ready-for-review");
                    var inProgress = tasks.Count(t => t.Status == "in-progress");
                    var todo = tasks.Count(t => t.Status == "todo");

                    var progress = total > 0 ? (int)Math.Round((double)approved / total * 100) : 0;
                    var reviewStatus = ComputeReviewStatus(project.Deadline, progress, tasks.Any(t => t.Status == "ready-for-review"));

                    var initials = members
                        .Where(m => m.User != null)
                        .Select(m => GetInitials(m.User!.Name))
                        .ToList();

                    results.Add(new LecturerGroupDto(
                        project.Id,
                        project.Name,
                        project.Description,
                        org.Name,
                        project.Deadline,
                        members.Count,
                        initials,
                        progress,
                        reviewStatus,
                        total,
                        approved,
                        inReview,
                        inProgress,
                        todo
                    ));
                }
            }

            return results;
        }

        // ── Get group detail ──────────────────────────────────────────
        public async Task<LecturerGroupDetailDto> GetGroupDetailAsync(Guid lecturerId, Guid projectId)
        {
            var project = await _projectRepo.GetByIdAsync(projectId);
            if (project == null)
                throw new InvalidOperationException("Project not found.");

            var members = await _projectRepo.GetMembersByProjectIdAsync(projectId);
            var tasks = await _projectRepo.GetTasksByProjectIdAsync(projectId);

            // Collect all comments from all tasks
            var allComments = new List<TaskCommentDto>();
            foreach (var task in tasks)
            {
                var comments = await _projectRepo.GetCommentsByTaskIdAsync(task.Id);
                allComments.AddRange(comments.Select(c => new TaskCommentDto(
                    c.Id,
                    c.UserId,
                    c.User?.Name ?? "Unknown",
                    c.User?.Role == "lecturer" ? "lecturer" : "student",
                    c.Content,
                    c.CreatedAt
                )));
            }

            var taskDtos = tasks.Select(t => new LecturerTaskDto(
                t.Id,
                t.Title,
                t.Description,
                t.Status,
                t.Priority,
                t.Assignee?.Name,
                t.StartDate,
                t.EndDate
            )).ToList();

            // Build contribution data
            var contributions = BuildContributions(tasks);

            var total = tasks.Count;
            var approved = tasks.Count(t => t.Status == "done" || t.Status == "approved");
            var progress = total > 0 ? (int)Math.Round((double)approved / total * 100) : 0;
            var reviewStatus = ComputeReviewStatus(project.Deadline, progress, tasks.Any(t => t.Status == "ready-for-review"));

            var memberNames = members
                .Where(m => m.User != null)
                .Select(m => m.User!.Name)
                .ToList();

            var orgName = project.Organization?.Name ?? "";

            return new LecturerGroupDetailDto(
                project.Id,
                project.Name,
                project.Description,
                orgName,
                project.Deadline,
                members.Count,
                memberNames,
                progress,
                reviewStatus,
                taskDtos,
                allComments,
                contributions
            );
        }

        // ── Approve task ──────────────────────────────────────────────
        public async Task ApproveTaskAsync(Guid lecturerId, Guid taskId)
        {
            var tasks = await FindTaskAndValidate(taskId);
            tasks.Status = "done";
            tasks.UpdatedAt = DateTimeOffset.UtcNow;
            await _projectRepo.UpdateTaskAsync(tasks);
        }

        // ── Request changes ───────────────────────────────────────────
        public async Task RequestChangesAsync(Guid lecturerId, Guid taskId)
        {
            var task = await FindTaskAndValidate(taskId);
            task.Status = "in-progress";
            task.UpdatedAt = DateTimeOffset.UtcNow;
            await _projectRepo.UpdateTaskAsync(task);
        }

        // ── Add comment ───────────────────────────────────────────────
        public async Task AddCommentAsync(Guid lecturerId, Guid taskId, string content)
        {
            var comment = new Entities.Projects.TaskComment
            {
                Id = Guid.NewGuid(),
                TaskId = taskId,
                UserId = lecturerId,
                Content = content,
                CreatedAt = DateTimeOffset.UtcNow
            };

            await _projectRepo.AddCommentAsync(comment);
        }

        // ── Notifications ─────────────────────────────────────────────
        public async Task<List<NotificationDto>> GetNotificationsAsync(Guid userId)
        {
            var notifications = await _notifRepo.GetByUserIdAsync(userId);
            return notifications.Select(n => new NotificationDto(
                n.Id,
                n.Type,
                n.Message,
                n.IsRead,
                n.CreatedAt
            )).ToList();
        }

        public Task MarkNotificationReadAsync(Guid userId, Guid notificationId)
        {
            return _notifRepo.MarkAsReadAsync(notificationId);
        }

        public Task MarkAllNotificationsReadAsync(Guid userId)
        {
            return _notifRepo.MarkAllAsReadAsync(userId);
        }

        // ── Private helpers ───────────────────────────────────────────

        private async Task<Entities.Projects.ProjectTask> FindTaskAndValidate(Guid taskId)
        {
            var task = await _projectRepo.GetTaskByIdAsync(taskId);
            if (task == null)
                throw new InvalidOperationException("Task not found.");
            return task;
        }

        private static string ComputeReviewStatus(DateOnly deadline, int progress, bool hasPendingReview)
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            if (deadline < today && progress < 100)
                return "overdue";
            var daysLeft = deadline.DayNumber - today.DayNumber;
            if (daysLeft <= 7 && progress < 60)
                return "at-risk";
            return "on-track";
        }

        private static string GetInitials(string name)
        {
            var parts = name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0) return "?";
            if (parts.Length == 1) return parts[0][..1].ToUpper();
            return (parts[0][..1] + parts[^1][..1]).ToUpper();
        }

        private static List<MemberContributionDto> BuildContributions(List<Entities.Projects.ProjectTask> tasks)
        {
            var byAssignee = tasks
                .Where(t => t.Assignee != null)
                .GroupBy(t => t.Assignee!.Name)
                .Select(g =>
                {
                    var all = g.ToList();
                    var approvedCount = all.Count(t => t.Status == "done" || t.Status == "approved");
                    var reviewCount = all.Count(t => t.Status == "ready-for-review");
                    var progressCount = all.Count(t => t.Status == "in-progress");
                    var todoCount = all.Count(t => t.Status == "todo");
                    var completion = all.Count > 0
                        ? (int)Math.Round((approvedCount + reviewCount * 0.7 + progressCount * 0.4) / all.Count * 100)
                        : 0;

                    return new MemberContributionDto(
                        g.Key,
                        all.Count,
                        approvedCount,
                        reviewCount,
                        progressCount,
                        todoCount,
                        completion
                    );
                })
                .OrderByDescending(c => c.CompletionPercent)
                .ToList();

            return byAssignee;
        }
    }
}
