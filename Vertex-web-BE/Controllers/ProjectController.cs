using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Vertex.Services.Interfaces;
using Vertex.Services.Models;
using Microsoft.AspNetCore.Http;

namespace Vertex_web_BE.Controllers
{
    [ApiController]
    [Route("api/orgs/{orgId}/projects")]
    [Authorize]
    public class ProjectController : ControllerBase
    {
        private readonly IProjectService _projectService;
        private readonly IFileService _fileService;

        public ProjectController(IProjectService projectService, IFileService fileService)
        {
            _projectService = projectService;
            _fileService = fileService;
        }

        private Guid GetUserId() =>
            Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        private Task EnsureCanAccessProjectAsync(Guid orgId, Guid projectId) =>
            _projectService.EnsureCanAccessProjectAsync(orgId, projectId, GetUserId());

        private static IActionResult Forbidden(UnauthorizedAccessException ex) =>
            new ObjectResult(new { error = ex.Message }) { StatusCode = StatusCodes.Status403Forbidden };

        // ── Projects ───────────────────────────────────────

        /// <summary>List all projects in an organization.</summary>
        [HttpGet]
        public async Task<IActionResult> List(Guid orgId)
        {
            try
            {
                var projects = await _projectService.ListProjectsAsync(orgId, GetUserId());
                return Ok(projects);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Forbidden(ex);
            }
        }

        /// <summary>Get full project details including tasks and members.</summary>
        [HttpGet("{projectId}")]
        public async Task<IActionResult> Detail(Guid orgId, Guid projectId)
        {
            try
            {
                var detail = await _projectService.GetProjectDetailAsync(orgId, projectId, GetUserId());
                return Ok(detail);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Forbidden(ex);
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
            catch (UnauthorizedAccessException ex)
            {
                return Forbidden(ex);
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
                await EnsureCanAccessProjectAsync(orgId, projectId);
                await _projectService.UpdateProjectAsync(projectId, input);
                return NoContent();
            }
            catch (UnauthorizedAccessException ex)
            {
                return Forbidden(ex);
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
                await EnsureCanAccessProjectAsync(orgId, projectId);
                await _projectService.DeleteProjectAsync(projectId);
                return NoContent();
            }
            catch (UnauthorizedAccessException ex)
            {
                return Forbidden(ex);
            }
            catch (InvalidOperationException ex)
            {
                return NotFound(new { error = ex.Message });
            }
        }

        // ── Tasks ──────────────────────────────────────────

        /// <summary>Get filtered tasks of a project.</summary>
        [HttpGet("{projectId}/tasks")]
        public async Task<IActionResult> GetFilteredTasks(
            Guid orgId, 
            Guid projectId, 
            [FromQuery] string? status = null, 
            [FromQuery] string? priority = null, 
            [FromQuery] Guid? assigneeId = null)
        {
            try
            {
                var tasks = await _projectService.GetFilteredTasksAsync(orgId, projectId, GetUserId(), status, priority, assigneeId);
                return Ok(tasks);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Forbidden(ex);
            }
            catch (InvalidOperationException ex)
            {
                return NotFound(new { error = ex.Message });
            }
        }

        /// <summary>Create a new task in a project.</summary>
        [HttpPost("{projectId}/tasks")]
        public async Task<IActionResult> CreateTask(Guid orgId, Guid projectId, [FromBody] CreateTaskInput input)
        {
            try
            {
                await EnsureCanAccessProjectAsync(orgId, projectId);
                var result = await _projectService.CreateTaskAsync(projectId, input);
                return Created($"/api/orgs/{orgId}/projects/{projectId}/tasks/{result.Id}", result);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Forbidden(ex);
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
                await EnsureCanAccessProjectAsync(orgId, projectId);
                var result = await _projectService.UpdateTaskAsync(taskId, input);
                return Ok(result);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Forbidden(ex);
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
                await EnsureCanAccessProjectAsync(orgId, projectId);
                await _projectService.DeleteTaskAsync(taskId);
                return NoContent();
            }
            catch (UnauthorizedAccessException ex)
            {
                return Forbidden(ex);
            }
            catch (InvalidOperationException ex)
            {
                return NotFound(new { error = ex.Message });
            }
        }

        // ── Members ────────────────────────────────────────

        /// <summary>List members of a project.</summary>
        [HttpGet("{projectId}/members")]
        public async Task<IActionResult> ListMembers(Guid orgId, Guid projectId)
        {
            try
            {
                var members = await _projectService.ListProjectMembersAsync(orgId, projectId, GetUserId());
                return Ok(members);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Forbidden(ex);
            }
            catch (InvalidOperationException ex)
            {
                return NotFound(new { error = ex.Message });
            }
        }

        /// <summary>Add a member to a project.</summary>
        [HttpPost("{projectId}/members")]
        public async Task<IActionResult> AddMember(Guid orgId, Guid projectId, [FromBody] AddProjectMemberInput input)
        {
            try
            {
                await EnsureCanAccessProjectAsync(orgId, projectId);
                var result = await _projectService.AddProjectMemberAsync(orgId, projectId, input);
                return Created($"/api/orgs/{orgId}/projects/{projectId}/members/{result.Id}", result);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Forbidden(ex);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        /// <summary>Update member role.</summary>
        [HttpPatch("{projectId}/members/{memberId}")]
        public async Task<IActionResult> UpdateMemberRole(Guid orgId, Guid projectId, Guid memberId, [FromBody] UpdateProjectMemberInput input)
        {
            try
            {
                await EnsureCanAccessProjectAsync(orgId, projectId);
                var result = await _projectService.UpdateProjectMemberRoleAsync(projectId, memberId, input);
                return Ok(result);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Forbidden(ex);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        /// <summary>Remove member from project.</summary>
        [HttpDelete("{projectId}/members/{memberId}")]
        public async Task<IActionResult> RemoveMember(Guid orgId, Guid projectId, Guid memberId)
        {
            try
            {
                await EnsureCanAccessProjectAsync(orgId, projectId);
                await _projectService.RemoveProjectMemberAsync(projectId, memberId);
                return NoContent();
            }
            catch (UnauthorizedAccessException ex)
            {
                return Forbidden(ex);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        // ── Files ──────────────────────────────────────────

        [HttpPost("{projectId}/files")]
        public async Task<IActionResult> UploadFile(Guid orgId, Guid projectId, IFormFile file, [FromQuery] string role = "Member")
        {
            try
            {
                await EnsureCanAccessProjectAsync(orgId, projectId);
                if (role != "Leader") return Forbid("Only Leader can upload files");
                var userId = GetUserId();
                using var stream = file.OpenReadStream();
                var result = await _fileService.UploadProjectFileAsync(projectId, userId, file.FileName, file.ContentType, file.Length, stream);
                return Ok(result);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Forbidden(ex);
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpGet("{projectId}/files")]
        public async Task<IActionResult> ListFiles(Guid orgId, Guid projectId)
        {
            try
            {
                await EnsureCanAccessProjectAsync(orgId, projectId);
                var files = await _fileService.GetProjectFilesAsync(projectId);
                return Ok(files);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Forbidden(ex);
            }
        }

        [HttpDelete("{projectId}/files/{fileId}")]
        public async Task<IActionResult> DeleteFile(Guid orgId, Guid projectId, Guid fileId, [FromQuery] string role = "Member")
        {
            try
            {
                var userId = GetUserId();
                await EnsureCanAccessProjectAsync(orgId, projectId);
                // Normally you would fetch user's role in the project here, but accepting from query/body as a shortcut for now
                await _fileService.DeleteProjectFileAsync(projectId, fileId, userId, role);
                return NoContent();
            }
            catch (UnauthorizedAccessException ex)
            {
                return Forbidden(ex);
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }
        // ── Links ──────────────────────────────────────────

        [HttpPost("{projectId}/links")]
        public async Task<IActionResult> AddLink(Guid orgId, Guid projectId, [FromBody] CreateProjectLinkInput input, [FromQuery] string role = "Member")
        {
            try
            {
                await EnsureCanAccessProjectAsync(orgId, projectId);
                if (role != "Leader") return Forbid("Only Leader can add links");
                var userId = GetUserId();
                var result = await _fileService.AddProjectLinkAsync(projectId, userId, input);
                return Ok(result);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Forbidden(ex);
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpGet("{projectId}/links")]
        public async Task<IActionResult> ListLinks(Guid orgId, Guid projectId)
        {
            try
            {
                await EnsureCanAccessProjectAsync(orgId, projectId);
                var links = await _fileService.GetProjectLinksAsync(projectId);
                return Ok(links);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Forbidden(ex);
            }
        }

        [HttpDelete("{projectId}/links/{linkId}")]
        public async Task<IActionResult> DeleteLink(Guid orgId, Guid projectId, Guid linkId, [FromQuery] string role = "Member")
        {
            try
            {
                var userId = GetUserId();
                await EnsureCanAccessProjectAsync(orgId, projectId);
                await _fileService.DeleteProjectLinkAsync(projectId, linkId, userId, role);
                return NoContent();
            }
            catch (UnauthorizedAccessException ex)
            {
                return Forbidden(ex);
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        // ── Task Attachments ───────────────────────────────────────

        [HttpPost("{projectId}/tasks/{taskId}/attachments/file")]
        public async Task<IActionResult> UploadTaskFile(Guid orgId, Guid projectId, Guid taskId, IFormFile file)
        {
            try
            {
                var userId = GetUserId();
                await EnsureCanAccessProjectAsync(orgId, projectId);
                using var stream = file.OpenReadStream();
                var result = await _fileService.UploadTaskFileAsync(taskId, userId, file.FileName, file.ContentType, file.Length, stream);
                return Ok(result);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Forbidden(ex);
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpPost("{projectId}/tasks/{taskId}/attachments/link")]
        public async Task<IActionResult> AddTaskLink(Guid orgId, Guid projectId, Guid taskId, [FromBody] CreateTaskLinkInput input)
        {
            try
            {
                var userId = GetUserId();
                await EnsureCanAccessProjectAsync(orgId, projectId);
                var result = await _fileService.AddTaskLinkAsync(taskId, userId, input);
                return Ok(result);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Forbidden(ex);
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpGet("{projectId}/tasks/{taskId}/attachments")]
        public async Task<IActionResult> ListTaskAttachments(Guid orgId, Guid projectId, Guid taskId)
        {
            try
            {
                await EnsureCanAccessProjectAsync(orgId, projectId);
                var attachments = await _fileService.GetTaskAttachmentsAsync(taskId);
                return Ok(attachments);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Forbidden(ex);
            }
        }

        [HttpDelete("{projectId}/tasks/{taskId}/attachments/{attachmentId}")]
        public async Task<IActionResult> DeleteTaskAttachment(Guid orgId, Guid projectId, Guid taskId, Guid attachmentId, [FromQuery] string role = "Member")
        {
            try
            {
                var userId = GetUserId();
                await EnsureCanAccessProjectAsync(orgId, projectId);
                await _fileService.DeleteTaskAttachmentAsync(taskId, attachmentId, userId, role);
                return NoContent();
            }
            catch (UnauthorizedAccessException ex)
            {
                return Forbidden(ex);
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpPost("{projectId}/tasks/{taskId}/attachments/{attachmentId}/promote")]
        public async Task<IActionResult> PromoteTaskAttachment(Guid orgId, Guid projectId, Guid taskId, Guid attachmentId, [FromQuery] string role = "Member")
        {
            try
            {
                await EnsureCanAccessProjectAsync(orgId, projectId);
                await _fileService.PromoteTaskAttachmentAsync(taskId, attachmentId, projectId, role);
                return NoContent();
            }
            catch (UnauthorizedAccessException ex)
            {
                return Forbidden(ex);
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        // ── Subtasks ──────────────────────────────────────

        /// <summary>List subtasks for a task.</summary>
        [HttpGet("{projectId}/tasks/{taskId}/subtasks")]
        public async Task<IActionResult> ListSubtasks(Guid orgId, Guid projectId, Guid taskId)
        {
            try
            {
                await EnsureCanAccessProjectAsync(orgId, projectId);
                var subtasks = await _projectService.ListSubtasksAsync(taskId);
                return Ok(subtasks);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Forbidden(ex);
            }
        }

        /// <summary>Create a subtask.</summary>
        [HttpPost("{projectId}/tasks/{taskId}/subtasks")]
        public async Task<IActionResult> CreateSubtask(Guid orgId, Guid projectId, Guid taskId, [FromBody] CreateSubtaskInput input)
        {
            try
            {
                await EnsureCanAccessProjectAsync(orgId, projectId);
                var result = await _projectService.CreateSubtaskAsync(taskId, GetUserId(), input);
                return Created($"/api/orgs/{orgId}/projects/{projectId}/tasks/{taskId}/subtasks/{result.Id}", result);
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(StatusCodes.Status403Forbidden, new { error = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        /// <summary>Update a subtask (title, completed, position).</summary>
        [HttpPut("{projectId}/tasks/{taskId}/subtasks/{subtaskId}")]
        public async Task<IActionResult> UpdateSubtask(Guid orgId, Guid projectId, Guid taskId, Guid subtaskId, [FromBody] UpdateSubtaskInput input)
        {
            try
            {
                await EnsureCanAccessProjectAsync(orgId, projectId);
                var result = await _projectService.UpdateSubtaskAsync(taskId, subtaskId, GetUserId(), input);
                return Ok(result);
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(StatusCodes.Status403Forbidden, new { error = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return NotFound(new { error = ex.Message });
            }
        }

        /// <summary>Delete a subtask.</summary>
        [HttpDelete("{projectId}/tasks/{taskId}/subtasks/{subtaskId}")]
        public async Task<IActionResult> DeleteSubtask(Guid orgId, Guid projectId, Guid taskId, Guid subtaskId)
        {
            try
            {
                await EnsureCanAccessProjectAsync(orgId, projectId);
                await _projectService.DeleteSubtaskAsync(taskId, subtaskId, GetUserId());
                return NoContent();
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(StatusCodes.Status403Forbidden, new { error = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return NotFound(new { error = ex.Message });
            }
        }

        // ── Task Comments ─────────────────────────────────

        /// <summary>List comments on a task.</summary>
        [HttpGet("{projectId}/tasks/{taskId}/comments")]
        public async Task<IActionResult> ListComments(Guid orgId, Guid projectId, Guid taskId)
        {
            try
            {
                await EnsureCanAccessProjectAsync(orgId, projectId);
                var comments = await _projectService.ListCommentsAsync(taskId);
                return Ok(comments);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Forbidden(ex);
            }
        }

        /// <summary>Add a comment to a task.</summary>
        [HttpPost("{projectId}/tasks/{taskId}/comments")]
        public async Task<IActionResult> AddComment(Guid orgId, Guid projectId, Guid taskId, [FromBody] CreateTaskCommentInput input)
        {
            try
            {
                await EnsureCanAccessProjectAsync(orgId, projectId);
                var result = await _projectService.AddCommentAsync(taskId, GetUserId(), input);
                return Created($"/api/orgs/{orgId}/projects/{projectId}/tasks/{taskId}/comments/{result.Id}", result);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Forbidden(ex);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        /// <summary>Delete your own comment.</summary>
        [HttpDelete("{projectId}/tasks/{taskId}/comments/{commentId}")]
        public async Task<IActionResult> DeleteComment(Guid orgId, Guid projectId, Guid taskId, Guid commentId)
        {
            try
            {
                await EnsureCanAccessProjectAsync(orgId, projectId);
                await _projectService.DeleteCommentAsync(commentId, GetUserId());
                return NoContent();
            }
            catch (UnauthorizedAccessException ex)
            {
                return Forbidden(ex);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }
    }
}

