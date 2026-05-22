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

        private static readonly Dictionary<string, int> StatusWeight = new()
        {
            ["todo"] = 0,
            ["in-progress"] = 50,
            ["ready-for-review"] = 80,
            ["done"] = 100,
        };

        public ProjectService(IProjectRepository projectRepo)
        {
            _projectRepo = projectRepo;
        }

        // ── Projects ───────────────────────────────────────

        public async Task<ProjectSummaryDto> CreateProjectAsync(Guid orgId, Guid creatorId, CreateProjectInput input)
        {
            if (string.IsNullOrWhiteSpace(input.Name))
                throw new InvalidOperationException("Project name is required.");

            var now = DateTimeOffset.UtcNow;
            var project = new Project
            {
                Id = Guid.NewGuid(),
                OrgId = orgId,
                Name = input.Name.Trim(),
                Description = input.Description?.Trim(),
                Deadline = DateTime.SpecifyKind(input.Deadline, DateTimeKind.Utc),
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
            if (input.Deadline.HasValue) project.Deadline = DateTime.SpecifyKind(input.Deadline.Value, DateTimeKind.Utc);
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
                StartDate = DateTime.SpecifyKind(input.StartDate, DateTimeKind.Utc),
                EndDate = DateTime.SpecifyKind(input.EndDate, DateTimeKind.Utc),
                Position = position,
                CreatedAt = now,
                UpdatedAt = now,
            };

            await _projectRepo.AddTaskAsync(task);

            // Re-fetch with Assignee navigation
            var saved = await _projectRepo.GetTaskByIdAsync(task.Id);
            return MapTask(saved!);
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
            if (input.StartDate.HasValue) task.StartDate = DateTime.SpecifyKind(input.StartDate.Value, DateTimeKind.Utc);
            if (input.EndDate.HasValue) task.EndDate = DateTime.SpecifyKind(input.EndDate.Value, DateTimeKind.Utc);
            if (input.Position.HasValue) task.Position = input.Position.Value;
            task.UpdatedAt = DateTimeOffset.UtcNow;

            await _projectRepo.UpdateTaskAsync(task);

            var updated = await _projectRepo.GetTaskByIdAsync(task.Id);
            return MapTask(updated!);
        }

        public async Task DeleteTaskAsync(Guid taskId)
        {
            var task = await _projectRepo.GetTaskByIdAsync(taskId);
            if (task == null) throw new InvalidOperationException("Task not found.");
            await _projectRepo.DeleteTaskAsync(task);
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
            t.StartDate, t.EndDate, t.Position, t.CreatedAt
        );

        private static ProjectMemberDto MapMember(ProjectMember m) => new(
            m.Id, m.UserId,
            m.User?.Name ?? "", m.User?.Email ?? "",
            m.User?.AvatarUrl ?? "", m.Role
        );
    }
}
