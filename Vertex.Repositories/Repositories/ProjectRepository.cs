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
        private readonly AppDbContext _db;

        public ProjectRepository(AppDbContext db)
        {
            _db = db;
        }

        // ── Projects ───────────────────────────────────────

        public Task<List<Project>> GetByOrgIdAsync(Guid orgId)
        {
            return _db.Projects
                .Where(p => p.OrgId == orgId)
                .Include(p => p.Tasks).ThenInclude(t => t.Assignee)
                .Include(p => p.Members).ThenInclude(m => m.User)
                .AsNoTracking()
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();
        }

        public Task<Project?> GetByIdAsync(Guid projectId)
        {
            return _db.Projects.AsNoTracking().FirstOrDefaultAsync(p => p.Id == projectId);
        }

        public Task<Project?> GetByIdWithDetailsAsync(Guid projectId)
        {
            return _db.Projects
                .Include(p => p.Tasks).ThenInclude(t => t.Assignee)
                .Include(p => p.Members).ThenInclude(m => m.User)
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == projectId);
        }

        public async Task AddAsync(Project project)
        {
            _db.Projects.Add(project);
            await _db.SaveChangesAsync();
        }

        public async Task UpdateAsync(Project project)
        {
            _db.Projects.Update(project);
            await _db.SaveChangesAsync();
        }

        public async Task DeleteAsync(Project project)
        {
            _db.Projects.Remove(project);
            await _db.SaveChangesAsync();
        }

        // ── Tasks ──────────────────────────────────────────

        public Task<ProjectTask?> GetTaskByIdAsync(Guid taskId)
        {
            return _db.ProjectTasks
                .Include(t => t.Assignee)
                .FirstOrDefaultAsync(t => t.Id == taskId);
        }

        public async Task AddTaskAsync(ProjectTask task)
        {
            _db.ProjectTasks.Add(task);
            await _db.SaveChangesAsync();
        }

        public async Task UpdateTaskAsync(ProjectTask task)
        {
            _db.ProjectTasks.Update(task);
            await _db.SaveChangesAsync();
        }

        public async Task DeleteTaskAsync(ProjectTask task)
        {
            _db.ProjectTasks.Remove(task);
            await _db.SaveChangesAsync();
        }

        public async Task<int> GetMaxTaskPositionAsync(Guid projectId, string status)
        {
            var max = await _db.ProjectTasks
                .Where(t => t.ProjectId == projectId && t.Status == status)
                .Select(t => (int?)t.Position)
                .MaxAsync();
            return max ?? 0;
        }

        // ── Members ────────────────────────────────────────

        public Task<ProjectMember?> GetMemberAsync(Guid projectId, Guid userId)
        {
            return _db.ProjectMembers
                .Include(m => m.User)
                .FirstOrDefaultAsync(m => m.ProjectId == projectId && m.UserId == userId);
        }

        public async Task AddMemberAsync(ProjectMember member)
        {
            _db.ProjectMembers.Add(member);
            await _db.SaveChangesAsync();
        }

        public async Task RemoveMemberAsync(ProjectMember member)
        {
            _db.ProjectMembers.Remove(member);
            await _db.SaveChangesAsync();
        }
    }
}
