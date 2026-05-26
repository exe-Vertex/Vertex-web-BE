using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Vertex.Entities.AuditLogs;
using Vertex.Repositories;
using Vertex.Services.Interfaces;
using Vertex.Services.Models;

namespace Vertex.Services.Services
{
    public class AdminService : IAdminService
    {
        private readonly AppDbContext _dbContext;

        public AdminService(AppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        // ── Get All Users (paginated, searchable, filterable) ─────────

        public async Task<AdminUserListResult> GetAllUsersAsync(string? search, string? status, int page, int pageSize)
        {
            var query = _dbContext.Users.AsNoTracking().AsQueryable();

            // Filter by status
            if (!string.IsNullOrWhiteSpace(status) && status != "all")
            {
                query = query.Where(u => u.Status == status);
            }

            // Search by name or email
            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim().ToLower();
                query = query.Where(u =>
                    u.Name.ToLower().Contains(term) ||
                    u.Email.ToLower().Contains(term));
            }

            var totalCount = await query.CountAsync();

            var users = await query
                .OrderByDescending(u => u.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // Resolve plan from organization membership (pick highest-level org plan)
            var userIds = users.Select(u => u.Id).ToList();
            var membershipPlans = await _dbContext.OrganizationMembers
                .AsNoTracking()
                .Where(m => userIds.Contains(m.UserId))
                .Include(m => m.Organization)
                .GroupBy(m => m.UserId)
                .Select(g => new
                {
                    UserId = g.Key,
                    Plan = g.Select(m => m.Organization.Plan).FirstOrDefault() ?? "free"
                })
                .ToListAsync();

            var planLookup = membershipPlans.ToDictionary(x => x.UserId, x => x.Plan);

            var dtos = users.Select(u => new AdminUserDto(
                Id: u.Id,
                Name: u.Name,
                Email: u.Email,
                Avatar: u.AvatarUrl ?? "",
                Status: u.Status ?? "active",
                Plan: planLookup.GetValueOrDefault(u.Id, "free"),
                CreatedAt: u.CreatedAt,
                AiQuota: u.AiQuota,
                AiUsed: u.AiUsed
            )).ToList();

            return new AdminUserListResult(dtos, totalCount, page, pageSize);
        }

        // ── Ban / Unban User ──────────────────────────────────────────

        public async Task<AdminUserDto> UpdateUserStatusAsync(Guid adminId, Guid targetUserId, string newStatus)
        {
            // Validate status value
            if (newStatus != "active" && newStatus != "banned")
                throw new InvalidOperationException("Status must be 'active' or 'banned'.");

            // Cannot ban yourself
            if (adminId == targetUserId)
                throw new InvalidOperationException("You cannot change your own account status.");

            var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == targetUserId);
            if (user == null)
                throw new InvalidOperationException("User not found.");

            // Cannot ban another admin
            if (user.Role == "admin" && newStatus == "banned")
                throw new InvalidOperationException("Cannot ban another admin account.");

            var previousStatus = user.Status;
            user.Status = newStatus;
            user.UpdatedAt = DateTimeOffset.UtcNow;

            // Write audit log
            var auditLog = new AuditLog
            {
                Id = Guid.NewGuid(),
                AdminId = adminId,
                Action = newStatus == "banned" ? "ban_user" : "unban_user",
                TargetUserId = targetUserId,
                Detail = $"Changed status from '{previousStatus}' to '{newStatus}'",
                CreatedAt = DateTimeOffset.UtcNow
            };
            _dbContext.AuditLogs.Add(auditLog);

            await _dbContext.SaveChangesAsync();

            // Resolve plan
            var plan = await _dbContext.OrganizationMembers
                .AsNoTracking()
                .Where(m => m.UserId == user.Id)
                .Include(m => m.Organization)
                .Select(m => m.Organization.Plan)
                .FirstOrDefaultAsync() ?? "free";

            return new AdminUserDto(
                Id: user.Id,
                Name: user.Name,
                Email: user.Email,
                Avatar: user.AvatarUrl ?? "",
                Status: user.Status,
                Plan: plan,
                CreatedAt: user.CreatedAt,
                AiQuota: user.AiQuota,
                AiUsed: user.AiUsed
            );
        }

        // ── Update AI Quota ───────────────────────────────────────────

        public async Task<AdminUserDto> UpdateUserAiQuotaAsync(Guid adminId, Guid targetUserId, int newQuota)
        {
            if (newQuota < 0)
                throw new InvalidOperationException("AI quota cannot be negative.");

            var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == targetUserId);
            if (user == null)
                throw new InvalidOperationException("User not found.");

            var previousQuota = user.AiQuota;
            user.AiQuota = newQuota;
            user.UpdatedAt = DateTimeOffset.UtcNow;

            // Write audit log
            var auditLog = new AuditLog
            {
                Id = Guid.NewGuid(),
                AdminId = adminId,
                Action = "change_quota",
                TargetUserId = targetUserId,
                Detail = $"Changed AI quota from {previousQuota} to {newQuota}",
                CreatedAt = DateTimeOffset.UtcNow
            };
            _dbContext.AuditLogs.Add(auditLog);

            await _dbContext.SaveChangesAsync();

            var plan = await _dbContext.OrganizationMembers
                .AsNoTracking()
                .Where(m => m.UserId == user.Id)
                .Include(m => m.Organization)
                .Select(m => m.Organization.Plan)
                .FirstOrDefaultAsync() ?? "free";

            return new AdminUserDto(
                Id: user.Id,
                Name: user.Name,
                Email: user.Email,
                Avatar: user.AvatarUrl ?? "",
                Status: user.Status,
                Plan: plan,
                CreatedAt: user.CreatedAt,
                AiQuota: user.AiQuota,
                AiUsed: user.AiUsed
            );
        }

        // ── Audit Logs ────────────────────────────────────────────────

        public async Task<AuditLogListResult> GetAuditLogsAsync(int page, int pageSize)
        {
            var query = _dbContext.AuditLogs
                .AsNoTracking()
                .Include(a => a.Admin)
                .Include(a => a.TargetUser)
                .OrderByDescending(a => a.CreatedAt);

            var totalCount = await query.CountAsync();

            var logs = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var dtos = logs.Select(a => new AuditLogDto(
                Id: a.Id,
                Admin: a.Admin?.Name ?? "System",
                Action: a.Action,
                Target: a.TargetUser?.Name,
                Detail: a.Detail ?? "",
                Timestamp: a.CreatedAt
            )).ToList();

            return new AuditLogListResult(dtos, totalCount, page, pageSize);
        }
    }
}
