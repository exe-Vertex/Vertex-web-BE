using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Vertex.Services.Models;

namespace Vertex.Services.Interfaces
{
    public interface IProjectService
    {
        Task<ProjectSummaryDto> CreateProjectAsync(Guid orgId, Guid creatorId, CreateProjectInput input);
        Task<List<ProjectSummaryDto>> ListProjectsAsync(Guid orgId);
        Task<ProjectDetailDto> GetProjectDetailAsync(Guid projectId);
        Task UpdateProjectAsync(Guid projectId, UpdateProjectInput input);
        Task DeleteProjectAsync(Guid projectId);

        // Tasks
        Task<TaskDto> CreateTaskAsync(Guid projectId, CreateTaskInput input);
        Task<TaskDto> UpdateTaskAsync(Guid taskId, UpdateTaskInput input);
        Task DeleteTaskAsync(Guid taskId);
        Task<List<TaskDto>> GetFilteredTasksAsync(Guid projectId, string? status, string? priority, Guid? assigneeId);

        // Members
        Task<List<ProjectMemberDto>> ListProjectMembersAsync(Guid projectId);
        Task<ProjectMemberDto> AddProjectMemberAsync(Guid orgId, Guid projectId, AddProjectMemberInput input);
        Task<ProjectMemberDto> UpdateProjectMemberRoleAsync(Guid projectId, Guid memberId, UpdateProjectMemberInput input);
        Task RemoveProjectMemberAsync(Guid projectId, Guid memberId);

        // Subtasks
        Task<List<SubtaskDto>> ListSubtasksAsync(Guid taskId);
        Task<SubtaskDto> CreateSubtaskAsync(Guid taskId, Guid userId, CreateSubtaskInput input);
        Task<SubtaskDto> UpdateSubtaskAsync(Guid taskId, Guid subtaskId, Guid userId, UpdateSubtaskInput input);
        Task DeleteSubtaskAsync(Guid taskId, Guid subtaskId, Guid userId);

        // Comments
        Task<List<ProjectTaskCommentDto>> ListCommentsAsync(Guid taskId);
        Task<ProjectTaskCommentDto> AddCommentAsync(Guid taskId, Guid userId, CreateTaskCommentInput input);
        Task DeleteCommentAsync(Guid commentId, Guid userId);
    }
}
