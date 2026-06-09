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

        // ── Members ────────────────────────────────────────

        /// <summary>List members of a project.</summary>
        [HttpGet("{projectId}/members")]
        public async Task<IActionResult> ListMembers(Guid orgId, Guid projectId)
        {
            try
            {
                var members = await _projectService.ListProjectMembersAsync(projectId);
                return Ok(members);
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
                var result = await _projectService.AddProjectMemberAsync(orgId, projectId, input);
                return Created($"/api/orgs/{orgId}/projects/{projectId}/members/{result.Id}", result);
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
                var result = await _projectService.UpdateProjectMemberRoleAsync(projectId, memberId, input);
                return Ok(result);
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
                await _projectService.RemoveProjectMemberAsync(projectId, memberId);
                return NoContent();
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
                if (role != "Leader") return Forbid("Only Leader can upload files");
                var userId = GetUserId();
                using var stream = file.OpenReadStream();
                var result = await _fileService.UploadProjectFileAsync(projectId, userId, file.FileName, file.ContentType, file.Length, stream);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpGet("{projectId}/files")]
        public async Task<IActionResult> ListFiles(Guid orgId, Guid projectId)
        {
            var files = await _fileService.GetProjectFilesAsync(projectId);
            return Ok(files);
        }

        [HttpDelete("{projectId}/files/{fileId}")]
        public async Task<IActionResult> DeleteFile(Guid orgId, Guid projectId, Guid fileId, [FromQuery] string role = "Member")
        {
            try
            {
                var userId = GetUserId();
                // Normally you would fetch user's role in the project here, but accepting from query/body as a shortcut for now
                await _fileService.DeleteProjectFileAsync(projectId, fileId, userId, role);
                return NoContent();
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
                if (role != "Leader") return Forbid("Only Leader can add links");
                var userId = GetUserId();
                var result = await _fileService.AddProjectLinkAsync(projectId, userId, input);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpGet("{projectId}/links")]
        public async Task<IActionResult> ListLinks(Guid orgId, Guid projectId)
        {
            var links = await _fileService.GetProjectLinksAsync(projectId);
            return Ok(links);
        }

        [HttpDelete("{projectId}/links/{linkId}")]
        public async Task<IActionResult> DeleteLink(Guid orgId, Guid projectId, Guid linkId, [FromQuery] string role = "Member")
        {
            try
            {
                var userId = GetUserId();
                await _fileService.DeleteProjectLinkAsync(projectId, linkId, userId, role);
                return NoContent();
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
                using var stream = file.OpenReadStream();
                var result = await _fileService.UploadTaskFileAsync(taskId, userId, file.FileName, file.ContentType, file.Length, stream);
                return Ok(result);
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
                var result = await _fileService.AddTaskLinkAsync(taskId, userId, input);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpGet("{projectId}/tasks/{taskId}/attachments")]
        public async Task<IActionResult> ListTaskAttachments(Guid orgId, Guid projectId, Guid taskId)
        {
            var attachments = await _fileService.GetTaskAttachmentsAsync(taskId);
            return Ok(attachments);
        }

        [HttpDelete("{projectId}/tasks/{taskId}/attachments/{attachmentId}")]
        public async Task<IActionResult> DeleteTaskAttachment(Guid orgId, Guid projectId, Guid taskId, Guid attachmentId, [FromQuery] string role = "Member")
        {
            try
            {
                var userId = GetUserId();
                await _fileService.DeleteTaskAttachmentAsync(taskId, attachmentId, userId, role);
                return NoContent();
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
                await _fileService.PromoteTaskAttachmentAsync(taskId, attachmentId, projectId, role);
                return NoContent();
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
            var subtasks = await _projectService.ListSubtasksAsync(taskId);
            return Ok(subtasks);
        }

        /// <summary>Create a subtask.</summary>
        [HttpPost("{projectId}/tasks/{taskId}/subtasks")]
        public async Task<IActionResult> CreateSubtask(Guid orgId, Guid projectId, Guid taskId, [FromBody] CreateSubtaskInput input)
        {
            try
            {
                var result = await _projectService.CreateSubtaskAsync(taskId, input);
                return Created($"/api/orgs/{orgId}/projects/{projectId}/tasks/{taskId}/subtasks/{result.Id}", result);
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
                var result = await _projectService.UpdateSubtaskAsync(subtaskId, input);
                return Ok(result);
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
                await _projectService.DeleteSubtaskAsync(subtaskId);
                return NoContent();
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
            var comments = await _projectService.ListCommentsAsync(taskId);
            return Ok(comments);
        }

        /// <summary>Add a comment to a task.</summary>
        [HttpPost("{projectId}/tasks/{taskId}/comments")]
        public async Task<IActionResult> AddComment(Guid orgId, Guid projectId, Guid taskId, [FromBody] CreateTaskCommentInput input)
        {
            try
            {
                var result = await _projectService.AddCommentAsync(taskId, GetUserId(), input);
                return Created($"/api/orgs/{orgId}/projects/{projectId}/tasks/{taskId}/comments/{result.Id}", result);
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
                await _projectService.DeleteCommentAsync(commentId, GetUserId());
                return NoContent();
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }
    }
}

