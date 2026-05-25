using System;

namespace Vertex.Entities.Projects
{
    public class ProjectFile
    {
        public Guid Id { get; set; }
        public Guid ProjectId { get; set; }
        public Guid UploadedBy { get; set; }
        public string FileName { get; set; } = string.Empty;
        public long FileSize { get; set; } = 0;
        public string? MimeType { get; set; }
        public string StoragePath { get; set; } = string.Empty;
        public DateTimeOffset CreatedAt { get; set; }

        // Navigation
        public Project Project { get; set; } = null!;
        public Users.User Uploader { get; set; } = null!;
    }
}
