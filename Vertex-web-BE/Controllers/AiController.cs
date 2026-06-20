using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Vertex.Services.Interfaces;
using Vertex.Services.Models;

namespace Vertex_web_BE.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class AiController : ControllerBase
    {
        private readonly IAiService _aiService;
        private readonly IAiSyncService _aiSyncService;

        public AiController(IAiService aiService, IAiSyncService aiSyncService)
        {
            _aiService = aiService;
            _aiSyncService = aiSyncService;
        }

        [HttpPost("chat")]
        public async Task<IActionResult> Chat([FromBody] ChatRequestDto request)
        {
            try
            {
                var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
                var orgId = request.OrgId ?? Guid.Empty;
                var result = await _aiService.ChatAsync(userId, orgId, request.Prompt);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("generate-plan")]
        public async Task<IActionResult> GeneratePlan([FromBody] GeneratePlanRequestDto request)
        {
            try
            {
                var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
                var planJson = await _aiService.GeneratePlanAsync(userId, request);
                return Ok(new { planSummary = planJson });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("history")]
        public async Task<IActionResult> GetHistory()
        {
            try
            {
                var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
                var history = await _aiService.GetHistoryAsync(userId);
                return Ok(history);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Syncs project and task data from the database into the in-memory Vector Store.
        /// This endpoint should be called whenever you want the AI to have up-to-date knowledge
        /// about your projects. The data is stored in RAM and will be lost on server restart.
        /// </summary>
        [HttpPost("sync-data/{orgId}")]
        public async Task<IActionResult> SyncData(Guid orgId)
        {
            try
            {
                var chunksCount = await _aiSyncService.SyncProjectDataAsync(orgId);
                return Ok(new
                {
                    message = $"Successfully synced {chunksCount} data chunks into the AI Vector Store.",
                    chunksCount
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("generate-subtasks")]
        public async Task<IActionResult> GenerateSubtasks([FromBody] GenerateSubtasksRequestDto request)
        {
            try
            {
                var subtasksJson = await _aiService.GenerateSubtasksAsync(request);
                return Ok(subtasksJson);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}
