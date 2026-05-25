using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Vertex.Entities.Projects;
using Vertex.Repositories.Interfaces;

namespace Vertex.Repositories.Repositories
{
    public class ProjectRepository : IProjectRepository
    {
        private readonly AppDbContext _dbContext;

        public ProjectRepository(AppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public Task<List<Project>> GetByOrgIdAsync(Guid orgId)
        {
            return _dbContext.Projects
                .AsNoTracking()
                .Where(x => x.OrgId == orgId)
                .ToListAsync();
        }

        public Task<Project?> GetByIdAsync(Guid id)
        {
            return _dbContext.Projects
                .AsNoTracking()
                .Include(x => x.Members)
                .Include(x => x.Tasks)
                .FirstOrDefaultAsync(x => x.Id == id);
        }

        public Task<List<ProjectMember>> GetMembersByProjectIdAsync(Guid projectId)
        {
            return _dbContext.ProjectMembers
                .AsNoTracking()
                .Where(x => x.ProjectId == projectId)
                .ToListAsync();
        }

        public Task<List<ProjectTask>> GetTasksByProjectIdAsync(Guid projectId)
        {
            return _dbContext.ProjectTasks
                .AsNoTracking()
                .Where(x => x.ProjectId == projectId)
                .Include(x => x.Comments)
                .ToListAsync();
        }

        public Task<List<TaskComment>> GetCommentsByTaskIdAsync(Guid taskId)
        {
            return _dbContext.TaskComments
                .AsNoTracking()
                .Where(x => x.TaskId == taskId)
                .ToListAsync();
        }

        public Task<ProjectTask?> GetTaskByIdAsync(Guid id)
        {
            return _dbContext.ProjectTasks
                .Include(x => x.Assignee)
                .Include(x => x.Comments)
                .FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task UpdateTaskAsync(ProjectTask task)
        {
            _dbContext.ProjectTasks.Update(task);
            await _dbContext.SaveChangesAsync();
        }

        public async Task AddCommentAsync(TaskComment comment)
        {
            _dbContext.TaskComments.Add(comment);
            await _dbContext.SaveChangesAsync();
        }
    }
}
