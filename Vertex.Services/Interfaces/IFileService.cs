using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Vertex.Services.Models;

namespace Vertex.Services.Interfaces
{
    public interface IFileService
    {
        Task<ProjectFileDto> UploadProjectFileAsync(Guid projectId, Guid uploaderId, string fileName, string contentType, long length, Stream fileStream);
        Task<List<ProjectFileDto>> GetProjectFilesAsync(Guid projectId);
        Task DeleteProjectFileAsync(Guid projectId, Guid fileId, Guid userId, string userRole);

        // Links
        Task<ProjectLinkDto> AddProjectLinkAsync(Guid projectId, Guid uploaderId, CreateProjectLinkInput input);
        Task<List<ProjectLinkDto>> GetProjectLinksAsync(Guid projectId);
        Task DeleteProjectLinkAsync(Guid projectId, Guid linkId, Guid userId, string userRole);

        // Task Attachments
        Task<TaskAttachmentDto> UploadTaskFileAsync(Guid taskId, Guid uploaderId, string fileName, string contentType, long length, Stream fileStream);
        Task<TaskAttachmentDto> AddTaskLinkAsync(Guid taskId, Guid uploaderId, CreateTaskLinkInput input);
        Task<List<TaskAttachmentDto>> GetTaskAttachmentsAsync(Guid taskId);
        Task DeleteTaskAttachmentAsync(Guid taskId, Guid attachmentId, Guid userId, string userRole);
        Task PromoteTaskAttachmentAsync(Guid taskId, Guid attachmentId, Guid projectId, string userRole);
    }
}
