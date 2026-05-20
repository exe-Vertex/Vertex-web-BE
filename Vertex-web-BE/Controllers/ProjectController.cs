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
    [Route("api/orgs/{orgId}/projects")]
    [Authorize]
    public class ProjectController : ControllerBase
    {
        private readonly IProjectService _projectService;

        public ProjectController(IProjectService projectService)
        {
            _projectService = projectService;
        }

        private Guid GetUserId() =>
            Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        // ── Projects ───────────────────────────────────────

        /// <summary>List all projects in an organization.</summary>
        [HttpGet]
        public async Task<IActionResult> List(Guid orgId)
        {
            var projects = await _projectService.ListProjectsAsync(orgId);
            return Ok(projects);
        }

        /// <summary>Get full project details including tasks and members.</summary>
        [HttpGet("{projectId}")]
        public async Task<IActionResult> Detail(Guid orgId, Guid projectId)
        {
            try
            {
                var detail = await _projectService.GetProjectDetailAsync(projectId);
                return Ok(detail);
            }
            catch (InvalidOperationException ex)
            {
                return NotFound(new { error = ex.Message });
            }
        }

        /// <summary>Create a new project. The caller becomes the Leader.</summary>
        [HttpPost]
        public async Task<IActionResult> Create(Guid orgId, [FromBody] CreateProjectInput input)
        {
            try
            {
                var result = await _projectService.CreateProjectAsync(orgId, GetUserId(), input);
                return Created($"/api/orgs/{orgId}/projects/{result.Id}", result);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        /// <summary>Update project name, description, or deadline.</summary>
        [HttpPut("{projectId}")]
        public async Task<IActionResult> Update(Guid orgId, Guid projectId, [FromBody] UpdateProjectInput input)
        {
            try
            {
                await _projectService.UpdateProjectAsync(projectId, input);
                return NoContent();
            }
            catch (InvalidOperationException ex)
            {
                return NotFound(new { error = ex.Message });
            }
        }

        /// <summary>Delete a project and all its tasks.</summary>
        [HttpDelete("{projectId}")]
        public async Task<IActionResult> Delete(Guid orgId, Guid projectId)
        {
            try
            {
                await _projectService.DeleteProjectAsync(projectId);
                return NoContent();
            }
            catch (InvalidOperationException ex)
            {
                return NotFound(new { error = ex.Message });
            }
        }

        // ── Tasks ──────────────────────────────────────────

        /// <summary>Create a new task in a project.</summary>
        [HttpPost("{projectId}/tasks")]
        public async Task<IActionResult> CreateTask(Guid orgId, Guid projectId, [FromBody] CreateTaskInput input)
        {
            try
            {
                var result = await _projectService.CreateTaskAsync(projectId, input);
                return Created($"/api/orgs/{orgId}/projects/{projectId}/tasks/{result.Id}", result);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        /// <summary>Update a task (title, status, assignee, etc.).</summary>
        [HttpPut("{projectId}/tasks/{taskId}")]
        public async Task<IActionResult> UpdateTask(Guid orgId, Guid projectId, Guid taskId, [FromBody] UpdateTaskInput input)
        {
            try
            {
                var result = await _projectService.UpdateTaskAsync(taskId, input);
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return NotFound(new { error = ex.Message });
            }
        }

        /// <summary>Delete a task.</summary>
        [HttpDelete("{projectId}/tasks/{taskId}")]
        public async Task<IActionResult> DeleteTask(Guid orgId, Guid projectId, Guid taskId)
        {
            try
            {
                await _projectService.DeleteTaskAsync(taskId);
                return NoContent();
            }
            catch (InvalidOperationException ex)
            {
                return NotFound(new { error = ex.Message });
            }
        }
    }
}
