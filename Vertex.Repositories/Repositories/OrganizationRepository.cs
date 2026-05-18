using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Vertex.Entities.Organizations;
using Vertex.Repositories.Interfaces;

namespace Vertex.Repositories.Repositories
{
    public class OrganizationRepository : IOrganizationRepository
    {
        private readonly AppDbContext _db;

        public OrganizationRepository(AppDbContext db)
        {
            _db = db;
        }

        // ── Organization CRUD ──────────────────────────────

        public Task<Organization?> GetByIdAsync(Guid id)
        {
            return _db.Organizations.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
        }

        public Task<Organization?> GetByIdWithMembersAsync(Guid id)
        {
            return _db.Organizations
                .Include(x => x.Members)
                    .ThenInclude(m => m.User)
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == id);
        }

        public Task<Organization?> GetBySlugAsync(string slug)
        {
            return _db.Organizations.AsNoTracking().FirstOrDefaultAsync(x => x.Slug == slug);
        }

        public Task<List<Organization>> GetByUserIdAsync(Guid userId)
        {
            return _db.Organizations
                .Where(o => o.Members.Any(m => m.UserId == userId))
                .AsNoTracking()
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();
        }

        public async Task AddAsync(Organization org)
        {
            _db.Organizations.Add(org);
            await _db.SaveChangesAsync();
        }

        public async Task UpdateAsync(Organization org)
        {
            _db.Organizations.Update(org);
            await _db.SaveChangesAsync();
        }

        // ── OrganizationMember ─────────────────────────────

        public Task<OrganizationMember?> GetMemberAsync(Guid orgId, Guid userId)
        {
            return _db.OrganizationMembers
                .Include(m => m.User)
                .FirstOrDefaultAsync(m => m.OrgId == orgId && m.UserId == userId);
        }

        public Task<OrganizationMember?> GetMemberByIdAsync(Guid memberId)
        {
            return _db.OrganizationMembers
                .Include(m => m.User)
                .FirstOrDefaultAsync(m => m.Id == memberId);
        }

        public Task<List<OrganizationMember>> GetMembersAsync(Guid orgId)
        {
            return _db.OrganizationMembers
                .Include(m => m.User)
                .Where(m => m.OrgId == orgId)
                .OrderBy(m => m.JoinedAt)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task AddMemberAsync(OrganizationMember member)
        {
            _db.OrganizationMembers.Add(member);
            await _db.SaveChangesAsync();
        }

        public async Task UpdateMemberAsync(OrganizationMember member)
        {
            _db.OrganizationMembers.Update(member);
            await _db.SaveChangesAsync();
        }

        public async Task RemoveMemberAsync(OrganizationMember member)
        {
            _db.OrganizationMembers.Remove(member);
            await _db.SaveChangesAsync();
        }

        public Task<int> CountMembersAsync(Guid orgId)
        {
            return _db.OrganizationMembers.CountAsync(m => m.OrgId == orgId);
        }
    }
}
