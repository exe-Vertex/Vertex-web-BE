using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Vertex.Entities.Projects;
using Vertex.Repositories;
using Vertex.Services.Interfaces;
using Vertex.Services.Models;

namespace Vertex.Services.Services
{
    public class FileService : IFileService
    {
        private readonly AppDbContext _dbContext;
        private readonly IStorageUsageService _storageUsageService;
        private readonly IAmazonS3 _s3Client;
        private readonly string? _storageBucketName;
        private readonly string _webRootPath;

        public FileService(
            AppDbContext dbContext,
            IStorageUsageService storageUsageService,
            IAmazonS3 s3Client,
            IConfiguration configuration)
        {
            _dbContext = dbContext;
            _storageUsageService = storageUsageService;
            _s3Client = s3Client;
            _storageBucketName = configuration["Storage:BucketName"];
            _webRootPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
        }

        public async Task<ProjectFileDto> UploadProjectFileAsync(Guid projectId, Guid uploaderId, string fileName, string contentType, long length, Stream fileStream)
        {
            var project = await _dbContext.Projects.FirstOrDefaultAsync(p => p.Id == projectId);
            if (project == null) throw new Exception("Project not found");

            var projectMember = await _dbContext.ProjectMembers.FirstOrDefaultAsync(m => m.ProjectId == projectId && m.UserId == uploaderId);
            if (projectMember == null) throw new Exception("You are not a member of this project");

            await _storageUsageService.EnsureCanStoreAsync(project.OrgId, length);

            var uploadsFolder = Path.Combine(_webRootPath, "uploads", "projects", projectId.ToString());
            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
            }

            var uniqueFileName = $"{Guid.NewGuid()}_{fileName}";
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await fileStream.CopyToAsync(stream);
            }

            var fileUrl = $"/uploads/projects/{projectId}/{uniqueFileName}";

            var projectFile = new ProjectFile
            {
                Id = Guid.NewGuid(),
                ProjectId = projectId,
                FileName = fileName,
                StoragePath = fileUrl,
                FileSize = length,
                MimeType = contentType,
                UploadedBy = uploaderId,
                CreatedAt = DateTimeOffset.UtcNow
            };

            _dbContext.ProjectFiles.Add(projectFile);
            await _dbContext.SaveChangesAsync();

            var uploader = await _dbContext.Users.FindAsync(uploaderId);

            return new ProjectFileDto(
                Id: projectFile.Id,
                ProjectId: projectFile.ProjectId,
                FileName: projectFile.FileName,
                FileUrl: projectFile.StoragePath,
                Size: projectFile.FileSize,
                MimeType: projectFile.MimeType,
                UploadedById: projectFile.UploadedBy,
                UploadedBy: uploader?.Name ?? "Unknown",
                UploadedAt: projectFile.CreatedAt,
                SizeLabel: FormatSize(projectFile.FileSize)
            );
        }

        public async Task<List<ProjectFileDto>> GetProjectFilesAsync(Guid projectId)
        {
            var files = await _dbContext.ProjectFiles
                .Include(f => f.Uploader)
                .Where(f => f.ProjectId == projectId)
                .OrderByDescending(f => f.CreatedAt)
                .ToListAsync();

            return files.Select(f => new ProjectFileDto(
                Id: f.Id,
                ProjectId: f.ProjectId,
                FileName: f.FileName,
                FileUrl: f.StoragePath,
                Size: f.FileSize,
                MimeType: f.MimeType,
                UploadedById: f.UploadedBy,
                UploadedBy: f.Uploader?.Name ?? "Unknown",
                UploadedAt: f.CreatedAt,
                SizeLabel: FormatSize(f.FileSize)
            )).ToList();
        }

        public async Task DeleteProjectFileAsync(Guid projectId, Guid fileId, Guid userId, string userRole)
        {
            var file = await _dbContext.ProjectFiles.FirstOrDefaultAsync(f => f.Id == fileId && f.ProjectId == projectId);
            if (file == null) throw new Exception("File not found");

            // Only Leader or the person who uploaded can delete
            if (userRole != "Leader" && file.UploadedBy != userId)
            {
                throw new Exception("You don't have permission to delete this file");
            }

            // Remove from disk
            var uploadsFolder = Path.Combine(_webRootPath, "uploads", "projects", projectId.ToString());
            var localFileName = Path.GetFileName(file.StoragePath);
            var filePath = Path.Combine(uploadsFolder, localFileName);

            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }

            _dbContext.ProjectFiles.Remove(file);
            await _dbContext.SaveChangesAsync();
        }

        private string FormatSize(long bytes)
        {
            string[] suffixes = { "B", "KB", "MB", "GB", "TB" };
            int i = 0;
            double dblSByte = bytes;
            while (dblSByte >= 1024 && i < suffixes.Length - 1)
            {
                dblSByte /= 1024;
                i++;
            }
            return $"{Math.Round(dblSByte, 2)} {suffixes[i]}";
        }

        // ── Links ──────────────────────────────────────────────────

        public async Task<ProjectLinkDto> AddProjectLinkAsync(Guid projectId, Guid uploaderId, CreateProjectLinkInput input)
        {
            var project = await _dbContext.Projects.FirstOrDefaultAsync(p => p.Id == projectId);
            if (project == null) throw new Exception("Project not found");

            var projectMember = await _dbContext.ProjectMembers.FirstOrDefaultAsync(m => m.ProjectId == projectId && m.UserId == uploaderId);
            if (projectMember == null) throw new Exception("You are not a member of this project");

            var link = new ProjectLink
            {
                Id = Guid.NewGuid(),
                ProjectId = projectId,
                Url = input.Url,
                Title = string.IsNullOrWhiteSpace(input.Title) ? input.Url : input.Title,
                UploadedBy = uploaderId,
                CreatedAt = DateTimeOffset.UtcNow
            };

            _dbContext.ProjectLinks.Add(link);
            await _dbContext.SaveChangesAsync();

            var uploader = await _dbContext.Users.FindAsync(uploaderId);

            return new ProjectLinkDto(
                Id: link.Id,
                ProjectId: link.ProjectId,
                Url: link.Url,
                Title: link.Title,
                UploadedById: link.UploadedBy,
                UploadedBy: uploader?.Name ?? "Unknown",
                UploadedAt: link.CreatedAt
            );
        }

        public async Task<List<ProjectLinkDto>> GetProjectLinksAsync(Guid projectId)
        {
            var links = await _dbContext.ProjectLinks
                .Include(l => l.Uploader)
                .Where(l => l.ProjectId == projectId)
                .OrderByDescending(l => l.CreatedAt)
                .ToListAsync();

            return links.Select(l => new ProjectLinkDto(
                Id: l.Id,
                ProjectId: l.ProjectId,
                Url: l.Url,
                Title: l.Title,
                UploadedById: l.UploadedBy,
                UploadedBy: l.Uploader?.Name ?? "Unknown",
                UploadedAt: l.CreatedAt
            )).ToList();
        }

        public async Task DeleteProjectLinkAsync(Guid projectId, Guid linkId, Guid userId, string userRole)
        {
            var link = await _dbContext.ProjectLinks.FirstOrDefaultAsync(l => l.Id == linkId && l.ProjectId == projectId);
            if (link == null) throw new Exception("Link not found");

            if (userRole != "Leader" && link.UploadedBy != userId)
            {
                throw new Exception("You don't have permission to delete this link");
            }

            _dbContext.ProjectLinks.Remove(link);
            await _dbContext.SaveChangesAsync();
        }

        // ── Task Attachments ───────────────────────────────────────
        public async Task<TaskAttachmentDto> UploadTaskFileAsync(Guid taskId, Guid uploaderId, string fileName, string contentType, long length, Stream fileStream)
        {
            var task = await _dbContext.ProjectTasks
                .Include(projectTask => projectTask.Project)
                .FirstOrDefaultAsync(projectTask => projectTask.Id == taskId);
            if (task == null) throw new Exception("Task not found");
            if (task.AssigneeId != uploaderId) throw new Exception("Only the assignee can attach files to this task");
            if (task.Project == null) throw new Exception("Project not found");

            await _storageUsageService.EnsureCanStoreAsync(task.Project.OrgId, length);

            var fileExt = Path.GetExtension(fileName);
            var newFileName = $"{Guid.NewGuid()}{fileExt}";
            string storageUrl;

            if (!string.IsNullOrWhiteSpace(_storageBucketName))
            {
                var objectKey = $"tasks/{taskId}/{newFileName}";
                await _s3Client.PutObjectAsync(new PutObjectRequest
                {
                    BucketName = _storageBucketName,
                    Key = objectKey,
                    InputStream = fileStream,
                    ContentType = contentType
                });
                storageUrl = $"s3://{_storageBucketName}/{objectKey}";
            }
            else
            {
                var uploadsFolder = Path.Combine(_webRootPath, "uploads", "tasks", taskId.ToString());
                if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

                var filePath = Path.Combine(uploadsFolder, newFileName);
                using var stream = new FileStream(filePath, FileMode.Create);
                await fileStream.CopyToAsync(stream);
                storageUrl = Path.Combine("uploads", "tasks", taskId.ToString(), newFileName).Replace("\\", "/");
            }

            var attachment = new TaskAttachment
            {
                Id = Guid.NewGuid(),
                TaskId = taskId,
                Type = "file",
                Title = fileName,
                Url = storageUrl,
                Size = length,
                MimeType = contentType,
                UploadedBy = uploaderId,
                CreatedAt = DateTimeOffset.UtcNow
            };

            _dbContext.TaskAttachments.Add(attachment);
            await _dbContext.SaveChangesAsync();

            var uploader = await _dbContext.Users.FindAsync(uploaderId);

            return new TaskAttachmentDto(
                Id: attachment.Id,
                TaskId: attachment.TaskId,
                Type: attachment.Type,
                Url: attachment.Type == "file" ? null : attachment.Url,
                Title: attachment.Title,
                Size: attachment.Size,
                SizeLabel: FormatSize(attachment.Size ?? 0),
                MimeType: attachment.MimeType,
                UploadedById: attachment.UploadedBy,
                UploadedBy: uploader?.Name ?? "Unknown",
                UploadedAt: attachment.CreatedAt
            );
        }

        public async Task<TaskAttachmentDto> AddTaskLinkAsync(Guid taskId, Guid uploaderId, CreateTaskLinkInput input)
        {
            var task = await _dbContext.ProjectTasks.FindAsync(taskId);
            if (task == null) throw new Exception("Task not found");
            if (task.AssigneeId != uploaderId) throw new Exception("Only the assignee can attach links to this task");

            var attachment = new TaskAttachment
            {
                Id = Guid.NewGuid(),
                TaskId = taskId,
                Type = "link",
                Url = input.Url,
                Title = string.IsNullOrWhiteSpace(input.Title) ? input.Url : input.Title,
                UploadedBy = uploaderId,
                CreatedAt = DateTimeOffset.UtcNow
            };

            _dbContext.TaskAttachments.Add(attachment);
            await _dbContext.SaveChangesAsync();

            var uploader = await _dbContext.Users.FindAsync(uploaderId);

            return new TaskAttachmentDto(
                Id: attachment.Id,
                TaskId: attachment.TaskId,
                Type: attachment.Type,
                Url: attachment.Url,
                Title: attachment.Title,
                Size: null,
                SizeLabel: null,
                MimeType: null,
                UploadedById: attachment.UploadedBy,
                UploadedBy: uploader?.Name ?? "Unknown",
                UploadedAt: attachment.CreatedAt
            );
        }

        public async Task<List<TaskAttachmentDto>> GetTaskAttachmentsAsync(Guid taskId)
        {
            var attachments = await _dbContext.TaskAttachments
                .Include(a => a.Uploader)
                .Where(a => a.TaskId == taskId)
                .OrderByDescending(a => a.CreatedAt)
                .ToListAsync();

            return attachments.Select(a => new TaskAttachmentDto(
                Id: a.Id,
                TaskId: a.TaskId,
                Type: a.Type,
                Url: a.Type == "file" ? null : a.Url,
                Title: a.Title,
                Size: a.Size,
                SizeLabel: a.Size.HasValue ? FormatSize(a.Size.Value) : null,
                MimeType: a.MimeType,
                UploadedById: a.UploadedBy,
                UploadedBy: a.Uploader?.Name ?? "Unknown",
                UploadedAt: a.CreatedAt
            )).ToList();
        }

        public async Task<string> GetTaskAttachmentDownloadUrlAsync(Guid projectId, Guid taskId, Guid attachmentId)
        {
            var attachment = await _dbContext.TaskAttachments
                .Include(a => a.Task)
                .FirstOrDefaultAsync(a => a.Id == attachmentId && a.TaskId == taskId && a.Task!.ProjectId == projectId);
            if (attachment == null) throw new Exception("Attachment not found");
            if (string.IsNullOrWhiteSpace(attachment.Url)) throw new FileNotFoundException("Attachment file is unavailable");
            if (attachment.Type == "link") return attachment.Url;

            if (TryParseS3Url(attachment.Url, out var bucketName, out var objectKey))
            {
                return _s3Client.GetPreSignedURL(new GetPreSignedUrlRequest
                {
                    BucketName = bucketName,
                    Key = objectKey,
                    Expires = DateTime.UtcNow.AddMinutes(5),
                    Verb = HttpVerb.GET
                });
            }

            var relativePath = attachment.Url.TrimStart('/').Replace("/", Path.DirectorySeparatorChar.ToString());
            var localPath = Path.Combine(_webRootPath, relativePath);
            if (!File.Exists(localPath))
            {
                throw new FileNotFoundException("The uploaded file is no longer available. Please upload it again.");
            }

            return "/" + attachment.Url.TrimStart('/');
        }
        public async Task DeleteTaskAttachmentAsync(Guid taskId, Guid attachmentId, Guid userId, string userRole)
        {
            var attachment = await _dbContext.TaskAttachments.FirstOrDefaultAsync(a => a.Id == attachmentId && a.TaskId == taskId);
            if (attachment == null) throw new Exception("Attachment not found");

            // Only the person who uploaded can delete it (which is the assignee)
            if (attachment.UploadedBy != userId)
            {
                throw new Exception("You don't have permission to delete this attachment");
            }

            if (attachment.Type == "file" && !string.IsNullOrEmpty(attachment.Url))
            {
                if (TryParseS3Url(attachment.Url, out var bucketName, out var objectKey))
                {
                    await _s3Client.DeleteObjectAsync(bucketName, objectKey);
                }
                else
                {
                    var filePath = Path.Combine(_webRootPath, attachment.Url.Replace("/", "\\"));
                    if (File.Exists(filePath)) File.Delete(filePath);
                }
            }

            _dbContext.TaskAttachments.Remove(attachment);
            await _dbContext.SaveChangesAsync();
        }

        public async Task PromoteTaskAttachmentAsync(Guid taskId, Guid attachmentId, Guid projectId, string userRole)
        {
            if (userRole != "Leader") throw new Exception("Only the Leader can promote attachments to the Project");

            var attachment = await _dbContext.TaskAttachments.FirstOrDefaultAsync(a => a.Id == attachmentId && a.TaskId == taskId);
            if (attachment == null) throw new Exception("Attachment not found");

            if (attachment.Type == "file")
            {
                var projectFile = new ProjectFile
                {
                    Id = Guid.NewGuid(),
                    ProjectId = projectId,
                    UploadedBy = attachment.UploadedBy ?? Guid.Empty, // Default to empty if null
                    FileName = attachment.Title ?? "Untitled",
                    FileSize = attachment.Size ?? 0,
                    MimeType = attachment.MimeType,
                    StoragePath = attachment.Url ?? string.Empty, // Reference same path
                    CreatedAt = DateTimeOffset.UtcNow
                };
                _dbContext.ProjectFiles.Add(projectFile);
            }
            else if (attachment.Type == "link")
            {
                var projectLink = new ProjectLink
                {
                    Id = Guid.NewGuid(),
                    ProjectId = projectId,
                    Url = attachment.Url ?? string.Empty,
                    Title = attachment.Title ?? "Untitled Link",
                    UploadedBy = attachment.UploadedBy,
                    CreatedAt = DateTimeOffset.UtcNow
                };
                _dbContext.ProjectLinks.Add(projectLink);
            }

            await _dbContext.SaveChangesAsync();
        }

        private static bool TryParseS3Url(string url, out string bucketName, out string objectKey)
        {
            bucketName = string.Empty;
            objectKey = string.Empty;
            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) || uri.Scheme != "s3") return false;

            bucketName = uri.Host;
            objectKey = uri.AbsolutePath.TrimStart('/');
            return !string.IsNullOrWhiteSpace(bucketName) && !string.IsNullOrWhiteSpace(objectKey);
        }
    }
}
