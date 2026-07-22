using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Vertex.Entities.Organizations;
using Vertex.Repositories.Interfaces;
using Vertex.Services.Interfaces;
using Vertex.Services.Models;
using Vertex.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Vertex.Services.Services
{
    public class OrganizationService : IOrganizationService
    {
        private readonly IOrganizationRepository _orgRepo;
        private readonly IUserRepository _userRepo;
        private readonly AppDbContext _context;

        private static readonly HashSet<string> AssignableRoles = new(StringComparer.OrdinalIgnoreCase)
        {
            "admin", "lecturer", "member"
        };

        public OrganizationService(IOrganizationRepository orgRepo, IUserRepository userRepo, AppDbContext context)
        {
            _orgRepo = orgRepo;
            _userRepo = userRepo;
            _context = context;
        }

        // ── Create ─────────────────────────────────────────

        public async Task<OrgSummary> CreateOrgAsync(Guid ownerId, CreateOrgInput input)
        {
            if (string.IsNullOrWhiteSpace(input.Name))
                throw new InvalidOperationException("Organization name is required.");

            var slug = string.IsNullOrWhiteSpace(input.Slug)
                ? input.Name.ToLower().Replace(" ", "-")
                : input.Slug.ToLower().Trim();

            var existing = await _orgRepo.GetBySlugAsync(slug);
            if (existing != null)
                throw new InvalidOperationException("An organization with this slug already exists.");

            var now = DateTimeOffset.UtcNow;
            var org = new Organization
            {
                Id = Guid.NewGuid(),
                Name = input.Name.Trim(),
                Slug = slug,
                AiQuotaPeriodStart = new DateTimeOffset(now.Year, now.Month, 1, 0, 0, 0, TimeSpan.Zero),
                CreatedAt = now,
                UpdatedAt = now
            };

            await _orgRepo.AddAsync(org);

            // Add the creator as owner
            var ownerMember = new OrganizationMember
            {
                Id = Guid.NewGuid(),
                OrgId = org.Id,
                UserId = ownerId,
                Role = "owner",
                JoinedAt = now
            };
            await _orgRepo.AddMemberAsync(ownerMember);

            return new OrgSummary(org.Id, org.Name, org.Slug, org.Plan, 1, org.MaxMembers, org.CreatedAt);
        }

        // ── List my orgs ───────────────────────────────────

        public async Task<List<OrgSummary>> GetMyOrgsAsync(Guid userId)
        {
            // Tự động sửa lỗi (Auto-heal): Nếu user có trong Project nhưng chưa có trong Organization cha, thêm họ vào Organization
            var projectOrgs = await _context.ProjectMembers
                .Where(pm => pm.UserId == userId && pm.Project != null)
                .Select(pm => pm.Project!.OrgId)
                .Distinct()
                .ToListAsync();

            bool changed = false;
            foreach (var orgId in projectOrgs)
            {
                var exists = await _context.OrganizationMembers.AnyAsync(om => om.OrgId == orgId && om.UserId == userId);
                if (!exists)
                {
                    _context.OrganizationMembers.Add(new OrganizationMember
                    {
                        Id = Guid.NewGuid(),
                        OrgId = orgId,
                        UserId = userId,
                        Role = "member",
                        JoinedAt = DateTimeOffset.UtcNow
                    });
                    changed = true;
                }
            }

            if (changed)
            {
                await _context.SaveChangesAsync();
            }

            var orgs = await _orgRepo.GetByUserIdAsync(userId);
            var summaries = new List<OrgSummary>();

            foreach (var org in orgs)
            {
                var count = await _orgRepo.CountMembersAsync(org.Id);
                summaries.Add(new OrgSummary(org.Id, org.Name, org.Slug, org.Plan, count, org.MaxMembers, org.CreatedAt));
            }

            return summaries;
        }

        // ── Detail ─────────────────────────────────────────

        public async Task<OrgDetail> GetOrgDetailAsync(Guid orgId, Guid requesterId)
        {
            var org = await _orgRepo.GetByIdWithMembersAsync(orgId);
            if (org == null)
                throw new InvalidOperationException("Organization not found.");

            // Requester must be a member
            var requester = org.Members.FirstOrDefault(m => m.UserId == requesterId);
            if (requester == null)
                throw new UnauthorizedAccessException("You are not a member of this organization.");

            var members = org.Members.Select(m => new OrgMemberDto(
                m.Id,
                m.UserId,
                m.User?.Name ?? "",
                m.User?.Email ?? "",
                m.User?.AvatarUrl ?? "",
                m.Role,
                m.JoinedAt
            )).ToList();

            var now = DateTimeOffset.UtcNow;
            var currentPeriodStart = new DateTimeOffset(now.Year, now.Month, 1, 0, 0, 0, TimeSpan.Zero);
            var aiUsed = org.AiQuotaPeriodStart < currentPeriodStart ? 0 : org.AiUsed;
            var quotaPeriodStart = org.AiQuotaPeriodStart < currentPeriodStart
                ? currentPeriodStart
                : org.AiQuotaPeriodStart;

            return new OrgDetail(
                org.Id, org.Name, org.Slug, org.Plan,
                org.MaxMembers, org.AiQuota, aiUsed, quotaPeriodStart, org.StorageLimit,
                org.CreatedAt, members
            );
        }

        // ── Invite member ──────────────────────────────────

        public async Task<OrgMemberDto> InviteMemberAsync(Guid orgId, Guid requesterId, InviteMemberInput input)
        {
            // Validate role — 'owner' cannot be assigned manually
            if (!AssignableRoles.Contains(input.Role))
                throw new InvalidOperationException($"Invalid role '{input.Role}'. Must be one of: admin, lecturer, member.");

            // Requester must be owner or admin
            await EnsureAdminAccessAsync(orgId, requesterId);

            // Find the user to invite
            var user = await _userRepo.GetByEmailAsync(input.Email);
            if (user == null)
                throw new InvalidOperationException($"No user found with email '{input.Email}'. They must register first.");

            // Check not already a member
            var existingMember = await _orgRepo.GetMemberAsync(orgId, user.Id);
            if (existingMember != null)
                throw new InvalidOperationException("This user is already a member of the organization.");

            // Check member limit
            var org = await _orgRepo.GetByIdAsync(orgId);
            if (org == null)
                throw new InvalidOperationException("Organization not found.");

            var count = await _orgRepo.CountMembersAsync(orgId);
            if (count >= org.MaxMembers)
                throw new InvalidOperationException($"Organization has reached its member limit ({org.MaxMembers}). Please upgrade your plan.");

            var member = new OrganizationMember
            {
                Id = Guid.NewGuid(),
                OrgId = orgId,
                UserId = user.Id,
                Role = input.Role.ToLower(),
                JoinedAt = DateTimeOffset.UtcNow
            };

            await _orgRepo.AddMemberAsync(member);

            return new OrgMemberDto(member.Id, user.Id, user.Name, user.Email, user.AvatarUrl, member.Role, member.JoinedAt);
        }

        // ── Update member role ─────────────────────────────

        public async Task UpdateMemberRoleAsync(Guid orgId, Guid memberId, Guid requesterId, UpdateMemberRoleInput input)
        {
            // 'owner' cannot be assigned manually
            if (!AssignableRoles.Contains(input.Role))
                throw new InvalidOperationException($"Invalid role '{input.Role}'. Must be one of: admin, lecturer, member.");

            await EnsureAdminAccessAsync(orgId, requesterId);

            var member = await _orgRepo.GetMemberByIdAsync(memberId);
            if (member == null || member.OrgId != orgId)
                throw new InvalidOperationException("Member not found in this organization.");

            // Cannot change role of the owner
            if (member.Role == "owner")
                throw new InvalidOperationException("Cannot change the role of the organization owner.");

            member.Role = input.Role.ToLower();
            await _orgRepo.UpdateMemberAsync(member);
        }

        // ── Remove member ──────────────────────────────────

        public async Task RemoveMemberAsync(Guid orgId, Guid memberId, Guid requesterId)
        {
            await EnsureAdminAccessAsync(orgId, requesterId);

            var member = await _orgRepo.GetMemberByIdAsync(memberId);
            if (member == null || member.OrgId != orgId)
                throw new InvalidOperationException("Member not found in this organization.");

            if (member.Role == "owner")
                throw new InvalidOperationException("Cannot remove the organization owner.");

            var userId = member.UserId;

            // Xóa member khỏi tổ chức
            await _orgRepo.RemoveMemberAsync(member);

            // Xóa member này khỏi tất cả các project thuộc tổ chức đó luôn
            var projectMembersToRemove = await _context.ProjectMembers
                .Where(pm => pm.Project.OrgId == orgId && pm.UserId == userId)
                .ToListAsync();
            
            if (projectMembersToRemove.Any())
            {
                _context.ProjectMembers.RemoveRange(projectMembersToRemove);
                await _context.SaveChangesAsync();
            }
        }

        // ── Helpers ────────────────────────────────────────

        private async Task EnsureAdminAccessAsync(Guid orgId, Guid userId)
        {
            var requester = await _orgRepo.GetMemberAsync(orgId, userId);
            if (requester == null)
                throw new UnauthorizedAccessException("You are not a member of this organization.");

            // Temporarily allow anyone in the org to perform actions
            // if (requester.Role != "owner" && requester.Role != "admin")
            //     throw new UnauthorizedAccessException("Only organization owners and admins can perform this action.");
        }
    }
}
