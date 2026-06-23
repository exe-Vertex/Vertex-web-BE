using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Vertex.Services.Interfaces;
using Vertex_web_BE.Models;

namespace Vertex_web_BE.Controllers
{
    [ApiController]
    [Route("api/lecturer")]
    [Authorize]
    public class LecturerController : ControllerBase
    {
        private readonly ILecturerService _lecturerService;

        public LecturerController(ILecturerService lecturerService)
        {
            _lecturerService = lecturerService;
        }

        /// <summary>Get all student groups for the current lecturer.</summary>
        [HttpGet("groups")]
        public async Task<IActionResult> GetGroups()
        {
            var userId = GetUserId();
            var groups = await _lecturerService.GetGroupsAsync(userId);
            return Ok(groups);
        }

        /// <summary>Get detailed info for a specific project/group.</summary>
        [HttpGet("groups/{projectId:guid}")]
        public async Task<IActionResult> GetGroupDetail(Guid projectId)
        {
            try
            {
                var userId = GetUserId();
                var detail = await _lecturerService.GetGroupDetailAsync(userId, projectId);
                return Ok(detail);
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (InvalidOperationException ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }

        /// <summary>Approve a task (move to done).</summary>
        [HttpPost("tasks/{taskId:guid}/approve")]
        public async Task<IActionResult> ApproveTask(Guid taskId)
        {
            try
            {
                var userId = GetUserId();
                await _lecturerService.ApproveTaskAsync(userId, taskId);
                return NoContent();
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>Request changes on a task (move back to in-progress).</summary>
        [HttpPost("tasks/{taskId:guid}/request-changes")]
        public async Task<IActionResult> RequestChanges(Guid taskId)
        {
            try
            {
                var userId = GetUserId();
                await _lecturerService.RequestChangesAsync(userId, taskId);
                return NoContent();
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>Add a comment/feedback to a task.</summary>
        [HttpPost("tasks/{taskId:guid}/comments")]
        public async Task<IActionResult> AddComment(Guid taskId, [FromBody] AddCommentRequest request)
        {
            try
            {
                var userId = GetUserId();
                await _lecturerService.AddCommentAsync(userId, taskId, request.Content);
                return NoContent();
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>Get notifications for the current user.</summary>
        [HttpGet("notifications")]
        public async Task<IActionResult> GetNotifications()
        {
            var userId = GetUserId();
            var notifications = await _lecturerService.GetNotificationsAsync(userId);
            return Ok(notifications);
        }

        /// <summary>Mark a notification as read.</summary>
        [HttpPut("notifications/{id:guid}/read")]
        public async Task<IActionResult> MarkRead(Guid id)
        {
            var userId = GetUserId();
            await _lecturerService.MarkNotificationReadAsync(userId, id);
            return NoContent();
        }

        /// <summary>Mark all notifications as read.</summary>
        [HttpPut("notifications/read-all")]
        public async Task<IActionResult> MarkAllRead()
        {
            var userId = GetUserId();
            await _lecturerService.MarkAllNotificationsReadAsync(userId);
            return NoContent();
        }

        // â”€â”€ Helper â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        private Guid GetUserId()
        {
            var value = User.FindFirstValue(ClaimTypes.NameIdentifier)
                        ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub);
            if (!Guid.TryParse(value, out var userId))
                throw new UnauthorizedAccessException("Invalid access token.");
            return userId;
        }
    }

    // â”€â”€ Request models â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
    public class AddCommentRequest
    {
        public string Content { get; set; } = string.Empty;
    }
}

