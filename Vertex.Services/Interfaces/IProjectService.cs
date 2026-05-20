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
    }
}
