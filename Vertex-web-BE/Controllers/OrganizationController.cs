using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Vertex.Services.Interfaces;
using Vertex.Services.Models;
using Vertex_web_BE.Models;

namespace Vertex_web_BE.Controllers
{
    [ApiController]
    [Route("api/orgs")]
    [Authorize]
    public class OrganizationController : ControllerBase
    {
        private readonly IOrganizationService _orgService;

        public OrganizationController(IOrganizationService orgService)
        {
            _orgService = orgService;
        }

        /// <summary>Create a new organization.</summary>
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateOrgRequest request)
        {
            try
            {
                var userId = GetUserId();
                var result = await _orgService.CreateOrgAsync(userId, new CreateOrgInput(request.Name, request.Slug));
                return Ok(ToSummaryResponse(result));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>List organizations for the current user.</summary>
        [HttpGet]
        public async Task<IActionResult> ListMyOrgs()
        {
            var userId = GetUserId();
            var orgs = await _orgService.GetMyOrgsAsync(userId);
            return Ok(orgs.Select(ToSummaryResponse).ToList());
        }

        /// <summary>Get organization details including members.</summary>
        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetDetail(Guid id)
        {
            try
            {
                var userId = GetUserId();
                var detail = await _orgService.GetOrgDetailAsync(id, userId);

                return Ok(new OrgDetailResponse
                {
                    Id = detail.Id,
                    Name = detail.Name,
                    Slug = detail.Slug,
                    Plan = detail.Plan,
                    MaxMembers = detail.MaxMembers,
                    AiQuota = detail.AiQuota,
                    AiUsed = detail.AiUsed,
                    AiQuotaPeriodStart = detail.AiQuotaPeriodStart,
                    StorageUsed = detail.StorageUsed,
                    StorageLimit = detail.StorageLimit,
                    CreatedAt = detail.CreatedAt,
                    Members = detail.Members.Select(m => new OrgMemberResponse
                    {
                        Id = m.Id,
                        UserId = m.UserId,
                        Name = m.Name,
                        Email = m.Email,
                        AvatarUrl = m.AvatarUrl,
                        Role = m.Role,
                        JoinedAt = m.JoinedAt
                    }).ToArray()
                });
            }
            catch (InvalidOperationException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, new { message = ex.Message });
            }
        }

        /// <summary>Invite a member to the organization by email.</summary>
        [HttpPost("{id:guid}/members")]
        public async Task<IActionResult> InviteMember(Guid id, [FromBody] InviteMemberRequest request)
        {
            try
            {
                var userId = GetUserId();
                var member = await _orgService.InviteMemberAsync(id, userId, new InviteMemberInput(request.Email, request.Role));

                return Ok(new OrgMemberResponse
                {
                    Id = member.Id,
                    UserId = member.UserId,
                    Name = member.Name,
                    Email = member.Email,
                    AvatarUrl = member.AvatarUrl,
                    Role = member.Role,
                    JoinedAt = member.JoinedAt
                });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, new { message = ex.Message });
            }
        }

        /// <summary>Update a member's role.</summary>
        [HttpPut("{id:guid}/members/{memberId:guid}")]
        public async Task<IActionResult> UpdateMemberRole(Guid id, Guid memberId, [FromBody] UpdateMemberRoleRequest request)
        {
            try
            {
                var userId = GetUserId();
                await _orgService.UpdateMemberRoleAsync(id, memberId, userId, new UpdateMemberRoleInput(request.Role));
                return NoContent();
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, new { message = ex.Message });
            }
        }

        /// <summary>Remove a member from the organization.</summary>
        [HttpDelete("{id:guid}/members/{memberId:guid}")]
        public async Task<IActionResult> RemoveMember(Guid id, Guid memberId)
        {
            try
            {
                var userId = GetUserId();
                await _orgService.RemoveMemberAsync(id, memberId, userId);
                return NoContent();
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, new { message = ex.Message });
            }
        }

        // ── Helper ─────────────────────────────────────────

        private Guid GetUserId()
        {
            var value = User.FindFirstValue(ClaimTypes.NameIdentifier)
                        ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub);
            if (!Guid.TryParse(value, out var userId))
                throw new UnauthorizedAccessException("Invalid access token.");
            return userId;
        }

        private static OrgSummaryResponse ToSummaryResponse(OrgSummary s) => new()
        {
            Id = s.Id,
            Name = s.Name,
            Slug = s.Slug,
            Plan = s.Plan,
            MemberCount = s.MemberCount,
            MaxMembers = s.MaxMembers,
            CreatedAt = s.CreatedAt
        };
    }
}
