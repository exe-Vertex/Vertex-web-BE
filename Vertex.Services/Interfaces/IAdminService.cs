using System;
using System.Threading.Tasks;
using Vertex.Services.Models;

namespace Vertex.Services.Interfaces
{
    public interface IAdminService
    {
        /// <summary>Get paginated list of all users with optional search and status filter.</summary>
        Task<AdminUserListResult> GetAllUsersAsync(string? search, string? status, int page, int pageSize);

        /// <summary>Ban or unban a user account. Writes an audit log entry.</summary>
        Task<AdminUserDto> UpdateUserStatusAsync(Guid adminId, Guid targetUserId, string newStatus);

        /// <summary>Update a user's AI quota. Writes an audit log entry.</summary>
        Task<AdminUserDto> UpdateUserAiQuotaAsync(Guid adminId, Guid targetUserId, int newQuota);

        /// <summary>Get paginated audit log entries.</summary>
        Task<AuditLogListResult> GetAuditLogsAsync(int page, int pageSize);

        /// <summary>Get real successful AI usage entries.</summary>
        Task<AdminAiUsageListResult> GetAiUsageAsync(int page, int pageSize);
    }
}
