using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Vertex.Entities.Projects;
using Vertex.Repositories.Interfaces;
using Vertex.Services.Interfaces;
using Vertex.Services.Models;

namespace Vertex.Services.Services
{
    public class ProjectService : IProjectService
    {
        private readonly IProjectRepository _projectRepo;
        private readonly IUserRepository _userRepo;
        private readonly IOrganizationRepository _orgRepo;
        private readonly ITaskNotifier _taskNotifier;

        private static readonly Dictionary<string, int> StatusWeight = new()
        {
            ["todo"] = 0,
            ["in-progress"] = 50,
            ["ready-for-review"] = 80,
            ["done"] = 100,
        };

        public ProjectService(IProjectRepository projectRepo, IUserRepository userRepo, IOrganizationRepository orgRepo, ITaskNotifier taskNotifier)
        {
            _projectRepo = projectRepo;
            _userRepo = userRepo;
            _orgRepo = orgRepo;
            _taskNotifier = taskNotifier;
        }

        // ── Projects ───────────────────────────────────────

        public async Task<ProjectSummaryDto> CreateProjectAsync(Guid orgId, Guid creatorId, CreateProjectInput input)
        {
            if (string.IsNullOrWhiteSpace(input.Name))
                throw new InvalidOperationException("Project name is required.");

            var org = await _orgRepo.GetByIdAsync(orgId);
            if (org == null)
                throw new InvalidOperationException("Organization not found.");

            var currentProjects = await _projectRepo.GetByOrgIdAsync(orgId);
            if (currentProjects.Count >= org.MaxProjects)
                throw new InvalidOperationException($"Tổ chức của bạn đã đạt giới hạn tối đa {org.MaxProjects} dự án. Vui lòng nâng cấp gói để tạo thêm.");

            var now = DateTimeOffset.UtcNow;
            var project = new Project
            {
                Id = Guid.NewGuid(),
                OrgId = orgId,
                Name = input.Name.Trim(),
                Description = input.Description?.Trim(),
                Deadline = input.Deadline.ToUniversalTime(),
                CreatedAt = now,
                UpdatedAt = now,
            };

            await _projectRepo.AddAsync(project);

            // Add creator as Leader
            var member = new ProjectMember
            {
                Id = Guid.NewGuid(),
                ProjectId = project.Id,
                UserId = creatorId,
                Role = "Leader",
                JoinedAt = now,
            };
            await _projectRepo.AddMemberAsync(member);

            return new ProjectSummaryDto(project.Id, project.Name, project.Description, project.Deadline, 0, 1, 0, project.CreatedAt);
        }

        public async Task<List<ProjectSummaryDto>> ListProjectsAsync(Guid orgId)
        {
            var projects = await _projectRepo.GetByOrgIdAsync(orgId);
            return projects.Select(p => new ProjectSummaryDto(
                p.Id, p.Name, p.Description, p.Deadline,
                p.Tasks.Count, p.Members.Count,
                ComputeProgress(p.Tasks), p.CreatedAt
            )).ToList();
        }

        public async Task<ProjectDetailDto> GetProjectDetailAsync(Guid projectId)
        {
            var project = await _projectRepo.GetByIdWithDetailsAsync(projectId);
            if (project == null) throw new InvalidOperationException("Project not found.");

            var tasks = project.Tasks.OrderBy(t => t.Position).Select(MapTask).ToList();
            var members = project.Members.Select(MapMember).ToList();

            return new ProjectDetailDto(
                project.Id, project.Name, project.Description, project.Deadline,
                ComputeProgress(project.Tasks), project.CreatedAt,
                tasks, members
            );
        }

        public async Task UpdateProjectAsync(Guid projectId, UpdateProjectInput input)
        {
            var project = await _projectRepo.GetByIdAsync(projectId);
            if (project == null) throw new InvalidOperationException("Project not found.");

            if (input.Name != null) project.Name = input.Name.Trim();
            if (input.Description != null) project.Description = input.Description.Trim();
            if (input.Deadline.HasValue) project.Deadline = input.Deadline.Value.ToUniversalTime();
            project.UpdatedAt = DateTimeOffset.UtcNow;

            await _projectRepo.UpdateAsync(project);
        }

        public async Task DeleteProjectAsync(Guid projectId)
        {
            var project = await _projectRepo.GetByIdAsync(projectId);
            if (project == null) throw new InvalidOperationException("Project not found.");
            await _projectRepo.DeleteAsync(project);
        }

        // ── Tasks ──────────────────────────────────────────

        public async Task<List<TaskDto>> GetFilteredTasksAsync(Guid projectId, string? status, string? priority, Guid? assigneeId)
        {
            var tasks = await _projectRepo.GetTasksByProjectIdAsync(projectId);
            var query = tasks.AsQueryable();

            if (!string.IsNullOrWhiteSpace(status))
            {
                query = query.Where(t => t.Status.Equals(status, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrWhiteSpace(priority))
            {
                query = query.Where(t => t.Priority.Equals(priority, StringComparison.OrdinalIgnoreCase));
            }

            if (assigneeId.HasValue)
            {
                query = query.Where(t => t.AssigneeId == assigneeId.Value);
            }

            return query.OrderBy(t => t.Position).Select(MapTask).ToList();
        }

        public async Task<TaskDto> CreateTaskAsync(Guid projectId, CreateTaskInput input)
        {
            if (string.IsNullOrWhiteSpace(input.Title))
                throw new InvalidOperationException("Task title is required.");

            var position = await _projectRepo.GetMaxTaskPositionAsync(projectId, input.Status) + 1;
            var now = DateTimeOffset.UtcNow;

            var task = new ProjectTask
            {
                Id = Guid.NewGuid(),
                ProjectId = projectId,
                Title = input.Title.Trim(),
                Description = input.Description?.Trim(),
                Status = input.Status,
                Priority = input.Priority,
                AssigneeId = input.AssigneeId == Guid.Empty ? null : input.AssigneeId,
                StartDate = input.StartDate.ToUniversalTime(),
                EndDate = input.EndDate.ToUniversalTime(),
                Position = position,
                SubmissionLink = input.SubmissionLink?.Trim(),
                CreatedAt = now,
                UpdatedAt = now,
            };

            await _projectRepo.AddTaskAsync(task);

            // Re-fetch with Assignee navigation
            var saved = await _projectRepo.GetTaskByIdAsync(task.Id);
            var resultDto = MapTask(saved!);
            
            await _taskNotifier.NotifyTaskCreatedAsync(projectId, resultDto);
            
            return resultDto;
        }

        public async Task<TaskDto> UpdateTaskAsync(Guid taskId, UpdateTaskInput input)
        {
            var task = await _projectRepo.GetTaskByIdAsync(taskId);
            if (task == null) throw new InvalidOperationException("Task not found.");

            if (input.Title != null) task.Title = input.Title.Trim();
            if (input.Description != null) task.Description = input.Description.Trim();
            if (input.Status != null) task.Status = input.Status;
            if (input.Priority != null) task.Priority = input.Priority;
            if (input.AssigneeId.HasValue) task.AssigneeId = input.AssigneeId.Value == Guid.Empty ? null : input.AssigneeId;
            if (input.StartDate.HasValue) task.StartDate = input.StartDate.Value.ToUniversalTime();
            if (input.EndDate.HasValue) task.EndDate = input.EndDate.Value.ToUniversalTime();
            if (input.Position.HasValue) task.Position = input.Position.Value;
            if (input.SubmissionLink != null) task.SubmissionLink = input.SubmissionLink.Trim();
            task.UpdatedAt = DateTimeOffset.UtcNow;

            await _projectRepo.UpdateTaskAsync(task);

            var updated = await _projectRepo.GetTaskByIdAsync(task.Id);
            var resultDto = MapTask(updated!);
            
            await _taskNotifier.NotifyTaskUpdatedAsync(task.ProjectId, resultDto);
            
            return resultDto;
        }

        public async Task DeleteTaskAsync(Guid taskId)
        {
            var task = await _projectRepo.GetTaskByIdAsync(taskId);
            if (task == null) throw new InvalidOperationException("Task not found.");
            
            var projectId = task.ProjectId;
            await _projectRepo.DeleteTaskAsync(task);
            
            await _taskNotifier.NotifyTaskDeletedAsync(projectId, taskId);
        }

        // ── Members ────────────────────────────────────────

        public async Task<List<ProjectMemberDto>> ListProjectMembersAsync(Guid projectId)
        {
            var project = await _projectRepo.GetByIdWithDetailsAsync(projectId);
            if (project == null) throw new InvalidOperationException("Project not found.");

            return project.Members.Select(MapMember).ToList();
        }

        public async Task<ProjectMemberDto> AddProjectMemberAsync(Guid orgId, Guid projectId, AddProjectMemberInput input)
        {
            var project = await _projectRepo.GetByIdAsync(projectId);
            if (project == null) throw new InvalidOperationException("Project not found.");

            Entities.Users.User? user = null;
            if (Guid.TryParse(input.EmailOrUserId, out var userId))
            {
                user = await _userRepo.GetByIdAsync(userId);
            }
            else
            {
                user = await _userRepo.GetByEmailAsync(input.EmailOrUserId);
            }

            if (user == null) throw new InvalidOperationException("User not found.");

            var orgMember = await _orgRepo.GetMemberAsync(orgId, user.Id);
            if (orgMember == null) throw new InvalidOperationException("User is not a member of the organization.");

            var existingMember = await _projectRepo.GetMemberAsync(projectId, user.Id);
            if (existingMember != null) throw new InvalidOperationException("User is already a member of this project.");

            var newMember = new ProjectMember
            {
                Id = Guid.NewGuid(),
                ProjectId = projectId,
                UserId = user.Id,
                Role = input.Role,
                ProjectSkills = input.ProjectSkills?.Trim(),
                JoinedAt = DateTimeOffset.UtcNow,
            };

            await _projectRepo.AddMemberAsync(newMember);

            // Fetch to get User populated
            var savedMember = await _projectRepo.GetMemberAsync(projectId, user.Id);
            return MapMember(savedMember!);
        }

        public async Task<ProjectMemberDto> UpdateProjectMemberRoleAsync(Guid projectId, Guid memberId, UpdateProjectMemberInput input)
        {
            var member = await _projectRepo.GetMemberAsync(projectId, memberId);
            if (member == null) throw new InvalidOperationException("Member not found in the project.");

            member.Role = input.Role;
            member.ProjectSkills = input.ProjectSkills?.Trim();
            await _projectRepo.UpdateMemberAsync(member);

            return MapMember(member);
        }

        public async Task RemoveProjectMemberAsync(Guid projectId, Guid memberId)
        {
            var project = await _projectRepo.GetByIdWithDetailsAsync(projectId);
            if (project == null) throw new InvalidOperationException("Project not found.");

            var member = await _projectRepo.GetMemberAsync(projectId, memberId);
            if (member == null) throw new InvalidOperationException("Member not found in the project.");

            if (member.Role == "Leader")
            {
                var leaderCount = project.Members.Count(m => m.Role == "Leader");
                if (leaderCount <= 1)
                {
                    throw new InvalidOperationException("Cannot remove the last Leader of the project.");
                }
            }

            await _projectRepo.RemoveMemberAsync(member);
        }

        // ── Helpers ────────────────────────────────────────

        private static int ComputeProgress(IEnumerable<ProjectTask> tasks)
        {
            var list = tasks.ToList();
            if (list.Count == 0) return 0;
            var total = list.Sum(t => StatusWeight.GetValueOrDefault(t.Status, 0));
            return (int)Math.Round((double)total / list.Count);
        }

        private static TaskDto MapTask(ProjectTask t) => new(
            t.Id, t.Title, t.Description, t.Status, t.Priority,
            t.Assignee != null
                ? new ProjectMemberDto(Guid.Empty, t.Assignee.Id, t.Assignee.Name, t.Assignee.Email, t.Assignee.AvatarUrl, "")
                : null,
            t.StartDate, t.EndDate, t.Position, t.SubmissionLink, t.CreatedAt
        );

        private static ProjectMemberDto MapMember(ProjectMember m) => new(
            m.Id, m.UserId,
            m.User?.Name ?? "", m.User?.Email ?? "",
            m.User?.AvatarUrl ?? "", m.Role, m.ProjectSkills
        );

        // ── Subtasks ────────────────────────────────────────

        public async Task<List<SubtaskDto>> ListSubtasksAsync(Guid taskId)
        {
            var subtasks = await _projectRepo.GetSubtasksByTaskIdAsync(taskId);
            return subtasks.Select(s => new SubtaskDto(s.Id, s.TaskId, s.Title, s.IsCompleted, s.Position)).ToList();
        }

        public async Task<SubtaskDto> CreateSubtaskAsync(Guid taskId, Guid userId, CreateSubtaskInput input)
        {
            if (string.IsNullOrWhiteSpace(input.Title))
                throw new InvalidOperationException("Subtask title is required.");

            var task = await _projectRepo.GetTaskByIdAsync(taskId);
            if (task == null) throw new InvalidOperationException("Task not found.");
            await EnsureCanManageSubtasksAsync(task, userId);

            var existing = await _projectRepo.GetSubtasksByTaskIdAsync(taskId);
            var maxPos = existing.Count > 0 ? existing.Max(s => s.Position) : -1;

            var subtask = new Subtask
            {
                Id = Guid.NewGuid(),
                TaskId = taskId,
                Title = input.Title.Trim(),
                IsCompleted = false,
                Position = maxPos + 1,
            };

            await _projectRepo.AddSubtaskAsync(subtask);
            return new SubtaskDto(subtask.Id, subtask.TaskId, subtask.Title, subtask.IsCompleted, subtask.Position);
        }

        public async Task<SubtaskDto> UpdateSubtaskAsync(Guid taskId, Guid subtaskId, Guid userId, UpdateSubtaskInput input)
        {
            var subtask = await _projectRepo.GetSubtaskByIdAsync(subtaskId);
            if (subtask == null) throw new InvalidOperationException("Subtask not found.");
            if (subtask.TaskId != taskId) throw new InvalidOperationException("Subtask does not belong to this task.");

            var task = await _projectRepo.GetTaskByIdAsync(taskId);
            if (task == null) throw new InvalidOperationException("Task not found.");
            await EnsureCanManageSubtasksAsync(task, userId);

            if (input.Title != null) subtask.Title = input.Title.Trim();
            if (input.IsCompleted.HasValue) subtask.IsCompleted = input.IsCompleted.Value;
            if (input.Position.HasValue) subtask.Position = input.Position.Value;

            await _projectRepo.UpdateSubtaskAsync(subtask);
            return new SubtaskDto(subtask.Id, subtask.TaskId, subtask.Title, subtask.IsCompleted, subtask.Position);
        }

        public async Task DeleteSubtaskAsync(Guid taskId, Guid subtaskId, Guid userId)
        {
            var subtask = await _projectRepo.GetSubtaskByIdAsync(subtaskId);
            if (subtask == null) throw new InvalidOperationException("Subtask not found.");
            if (subtask.TaskId != taskId) throw new InvalidOperationException("Subtask does not belong to this task.");

            var task = await _projectRepo.GetTaskByIdAsync(taskId);
            if (task == null) throw new InvalidOperationException("Task not found.");
            await EnsureCanManageSubtasksAsync(task, userId);

            await _projectRepo.DeleteSubtaskAsync(subtask);
        }

        // ── Comments ────────────────────────────────────────

        private async Task EnsureCanManageSubtasksAsync(ProjectTask task, Guid userId)
        {
            if (task.AssigneeId == userId) return;

            var member = await _projectRepo.GetMemberAsync(task.ProjectId, userId);
            if (member?.Role == "Leader") return;

            throw new UnauthorizedAccessException("Only the task assignee or project Leader can manage subtasks.");
        }

        public async Task<List<ProjectTaskCommentDto>> ListCommentsAsync(Guid taskId)
        {
            var comments = await _projectRepo.GetCommentsByTaskIdAsync(taskId);
            return comments.Select(c => new ProjectTaskCommentDto(
                c.Id, c.TaskId, c.UserId,
                c.User?.Name ?? "", c.User?.AvatarUrl ?? "",
                c.Content, c.CreatedAt
            )).ToList();
        }

        public async Task<ProjectTaskCommentDto> AddCommentAsync(Guid taskId, Guid userId, CreateTaskCommentInput input)
        {
            if (string.IsNullOrWhiteSpace(input.Content))
                throw new InvalidOperationException("Comment content is required.");

            var task = await _projectRepo.GetTaskByIdAsync(taskId);
            if (task == null) throw new InvalidOperationException("Task not found.");

            var comment = new TaskComment
            {
                Id = Guid.NewGuid(),
                TaskId = taskId,
                UserId = userId,
                Content = input.Content.Trim(),
                CreatedAt = DateTimeOffset.UtcNow,
            };

            await _projectRepo.AddCommentAsync(comment);

            var user = await _userRepo.GetByIdAsync(userId);
            return new ProjectTaskCommentDto(
                comment.Id, comment.TaskId, comment.UserId,
                user?.Name ?? "", user?.AvatarUrl ?? "",
                comment.Content, comment.CreatedAt
            );
        }

        public async Task DeleteCommentAsync(Guid commentId, Guid userId)
        {
            var comment = await _projectRepo.GetCommentByIdAsync(commentId);
            if (comment == null) throw new InvalidOperationException("Comment not found.");
            if (comment.UserId != userId)
                throw new InvalidOperationException("You can only delete your own comments.");
            await _projectRepo.DeleteCommentAsync(comment);
        }
    }
}
