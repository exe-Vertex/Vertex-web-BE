using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Vertex.Entities.Projects;

namespace Vertex.Repositories.Interfaces
{
    public interface IProjectRepository
    {
        // Projects
        Task<List<Project>> GetByOrgIdAsync(Guid orgId);
        Task<Project?> GetByIdAsync(Guid projectId);
        Task<Project?> GetByIdWithDetailsAsync(Guid projectId);
        Task AddAsync(Project project);
        Task UpdateAsync(Project project);
        Task DeleteAsync(Project project);

        // Tasks
        Task<ProjectTask?> GetTaskByIdAsync(Guid taskId);
        Task AddTaskAsync(ProjectTask task);
        Task UpdateTaskAsync(ProjectTask task);
        Task DeleteTaskAsync(ProjectTask task);
        Task<int> GetMaxTaskPositionAsync(Guid projectId, string status);

        // Members
        Task<ProjectMember?> GetMemberAsync(Guid projectId, Guid userId);
        Task AddMemberAsync(ProjectMember member);
        Task RemoveMemberAsync(ProjectMember member);
    }
}
