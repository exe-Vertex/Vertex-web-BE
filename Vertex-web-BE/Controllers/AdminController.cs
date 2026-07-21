using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Vertex.Services.Interfaces;
using Vertex.Services.Models;

namespace Vertex_web_BE.Controllers
{
    [ApiController]
    [Route("api/admin")]
    [Authorize]
    public class AdminController : ControllerBase
    {
        private readonly IAdminService _adminService;

        public AdminController(IAdminService adminService)
        {
            _adminService = adminService;
        }

        /// <summary>Get paginated list of all users. Admin only.</summary>
        [HttpGet("users")]
        public async Task<IActionResult> GetUsers(
            [FromQuery] string? search,
            [FromQuery] string? status,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50)
        {
            var adminId = GetAdminUserId();

            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 10;
            if (pageSize > 200) pageSize = 200;

            var result = await _adminService.GetAllUsersAsync(search, status, page, pageSize);
            return Ok(result);
        }

        /// <summary>Ban or unban a user. Admin only.</summary>
        [HttpPut("users/{id:guid}/status")]
        public async Task<IActionResult> UpdateUserStatus(Guid id, [FromBody] UpdateUserStatusRequest request)
        {
            try
            {
                var adminId = GetAdminUserId();
                var result = await _adminService.UpdateUserStatusAsync(adminId, id, request.Status);
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>Update a user's AI quota. Admin only.</summary>
        [HttpPut("users/{id:guid}/quota")]
        public async Task<IActionResult> UpdateUserQuota(Guid id, [FromBody] UpdateUserQuotaRequest request)
        {
            try
            {
                var adminId = GetAdminUserId();
                var result = await _adminService.UpdateUserAiQuotaAsync(adminId, id, request.AiQuota);
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>Get paginated audit logs. Admin only.</summary>
        [HttpGet("audit-logs")]
        public async Task<IActionResult> GetAuditLogs(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50)
        {
            var adminId = GetAdminUserId(); // validates admin role

            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 10;
            if (pageSize > 200) pageSize = 200;

            var result = await _adminService.GetAuditLogsAsync(page, pageSize);
            return Ok(result);
        }

        /// <summary>Get real successful AI usage history. Admin only.</summary>
        [HttpGet("ai-usage")]
        public async Task<IActionResult> GetAiUsage(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 200)
        {
            GetAdminUserId();

            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 10;
            if (pageSize > 500) pageSize = 500;

            var result = await _adminService.GetAiUsageAsync(page, pageSize);
            return Ok(result);
        }
        // ── Helper ─────────────────────────────────────────

        /// <summary>Extract user ID from JWT and verify the caller is an admin.</summary>
        private Guid GetAdminUserId()
        {
            var value = User.FindFirstValue(ClaimTypes.NameIdentifier)
                        ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub);
            if (!Guid.TryParse(value, out var userId))
                throw new UnauthorizedAccessException("Invalid access token.");

            // Verify admin role from JWT claims
            var role = User.FindFirstValue(ClaimTypes.Role)
                       ?? User.FindFirstValue("role");
            if (role != "admin")
                throw new UnauthorizedAccessException("Admin access required.");

            return userId;
        }
    }

    // ── Request models ────────────────────────────────────

    public class UpdateUserStatusRequest
    {
        public string Status { get; set; } = string.Empty;
    }

    public class UpdateUserQuotaRequest
    {
        public int AiQuota { get; set; }
    }
}
