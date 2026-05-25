using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Vertex.Services.Interfaces;
using Vertex_web_BE.Models;

namespace Vertex_web_BE.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class InvitationsController : ControllerBase
    {
        private readonly IInvitationService _invitationService;

        public InvitationsController(IInvitationService invitationService)
        {
            _invitationService = invitationService;
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> CreateInvitation([FromBody] CreateInvitationRequest request)
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(userIdString, out var userId)) return Unauthorized();

            var invitation = await _invitationService.CreateInvitationAsync(
                userId, request.Email, request.TargetType, request.TargetId, request.Role);

            return Ok(new { Message = "Invitation sent successfully", InvitationId = invitation.Id });
        }

        [HttpGet("verify")]
        public async Task<IActionResult> VerifyToken([FromQuery] string token)
        {
            if (string.IsNullOrEmpty(token)) return BadRequest("Token is required");

            try
            {
                var invitation = await _invitationService.VerifyTokenAsync(token);
                return Ok(new
                {
                    Email = invitation.Email,
                    TargetType = invitation.TargetType,
                    TargetId = invitation.TargetId,
                    Role = invitation.Role
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [HttpPost("accept")]
        [Authorize]
        public async Task<IActionResult> AcceptInvitation([FromBody] AcceptInvitationRequest request)
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(userIdString, out var userId)) return Unauthorized();

            try
            {
                await _invitationService.AcceptInvitationAsync(userId, request.Token);
                return Ok(new { Message = "Invitation accepted successfully" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }
    }
}
