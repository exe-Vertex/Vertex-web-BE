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

            // Resolve the highest plan across all organizations the user belongs to.
            var userIds = users.Select(u => u.Id).ToList();
            var membershipPlans = await _dbContext.OrganizationMembers
                .AsNoTracking()
                .Where(m => userIds.Contains(m.UserId))
                .Select(m => new
                {
                    m.UserId,
                    m.Organization.Plan
                })
                .ToListAsync();

            var planLookup = membershipPlans
                .GroupBy(x => x.UserId)
                .ToDictionary(
                    group => group.Key,
                    group => group
                        .OrderByDescending(item => GetPlanPriority(item.Plan))
                        .Select(item => item.Plan)
                        .FirstOrDefault() ?? "free");

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

            var plan = await GetHighestPlanForUserAsync(user.Id);

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

            var plan = await GetHighestPlanForUserAsync(user.Id);

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

        public async Task<List<AdminOrganizationQuotaDto>> GetOrganizationQuotasAsync()
        {
            var now = DateTimeOffset.UtcNow;
            var periodStart = new DateTimeOffset(now.Year, now.Month, 1, 0, 0, 0, TimeSpan.Zero);

            await _dbContext.Organizations
                .Where(org => org.AiQuotaPeriodStart < periodStart)
                .ExecuteUpdateAsync(update => update
                    .SetProperty(org => org.AiUsed, 0)
                    .SetProperty(org => org.AiQuotaPeriodStart, periodStart)
                    .SetProperty(org => org.UpdatedAt, now));

            return await _dbContext.Organizations
                .AsNoTracking()
                .OrderByDescending(org => org.CreatedAt)
                .Select(org => new AdminOrganizationQuotaDto(
                    org.Id,
                    org.Name,
                    org.Plan,
                    org.AiQuota,
                    org.AiUsed,
                    org.AiQuotaPeriodStart,
                    org.Members.Count))
                .ToListAsync();
        }

        public async Task<AdminOrganizationQuotaDto> UpdateOrganizationAiQuotaAsync(
            Guid adminId,
            Guid orgId,
            int newQuota)
        {
            if (newQuota < 0)
                throw new InvalidOperationException("AI quota cannot be negative.");

            var org = await _dbContext.Organizations
                .Include(item => item.Members)
                .FirstOrDefaultAsync(item => item.Id == orgId);
            if (org == null)
                throw new InvalidOperationException("Organization not found.");

            var previousQuota = org.AiQuota;
            org.AiQuota = newQuota;
            org.UpdatedAt = DateTimeOffset.UtcNow;

            _dbContext.AuditLogs.Add(new AuditLog
            {
                Id = Guid.NewGuid(),
                AdminId = adminId,
                Action = "change_quota",
                TargetUserId = null,
                Detail = $"Changed AI quota for organization '{org.Name}' from {previousQuota} to {newQuota}",
                CreatedAt = DateTimeOffset.UtcNow
            });

            await _dbContext.SaveChangesAsync();

            return new AdminOrganizationQuotaDto(
                org.Id,
                org.Name,
                org.Plan,
                org.AiQuota,
                org.AiUsed,
                org.AiQuotaPeriodStart,
                org.Members.Count);
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
        public async Task<AdminAiUsageListResult> GetAiUsageAsync(int page, int pageSize)
        {
            var query = _dbContext.AiHistories
                .AsNoTracking()
                .Include(history => history.User)
                .OrderByDescending(history => history.CreatedAt);

            var totalCount = await query.CountAsync();
            var histories = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var entries = histories.Select(history => new AdminAiUsageDto(
                Id: history.Id,
                UserId: history.UserId,
                UserName: history.User.Name,
                Prompt: history.Prompt,
                PlanSummary: history.PlanSummary ?? string.Empty,
                CreatedAt: history.CreatedAt,
                UsageUnits: 1
            )).ToList();

            return new AdminAiUsageListResult(entries, totalCount, page, pageSize);
        }

        private async Task<string> GetHighestPlanForUserAsync(Guid userId)
        {
            var plans = await _dbContext.OrganizationMembers
                .AsNoTracking()
                .Where(member => member.UserId == userId)
                .Select(member => member.Organization.Plan)
                .ToListAsync();

            return plans
                .OrderByDescending(GetPlanPriority)
                .FirstOrDefault() ?? "free";
        }

        private static int GetPlanPriority(string? plan) => plan?.ToLowerInvariant() switch
        {
            "enterprise" => 4,
            "business" => 3,
            "pro" => 2,
            "paid" => 1,
            _ => 0
        };
    }
}
