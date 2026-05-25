using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Vertex.Entities.Projects;

namespace Vertex.Repositories.Interfaces
{
    public interface IProjectRepository
    {
        Task<List<Project>> GetByOrgIdAsync(Guid orgId);
        Task<Project?> GetByIdAsync(Guid id);
        Task<List<ProjectMember>> GetMembersByProjectIdAsync(Guid projectId);
        Task<List<ProjectTask>> GetTasksByProjectIdAsync(Guid projectId);
        Task<List<TaskComment>> GetCommentsByTaskIdAsync(Guid taskId);
        Task<ProjectTask?> GetTaskByIdAsync(Guid id);
        Task UpdateTaskAsync(ProjectTask task);
        Task AddCommentAsync(TaskComment comment);
    }
}
