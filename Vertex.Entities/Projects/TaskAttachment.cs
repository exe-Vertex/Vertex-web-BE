using System;
using Vertex.Entities.Users;

namespace Vertex.Entities.Projects
{
    public class TaskAttachment
    {
        public Guid Id { get; set; }
        public Guid TaskId { get; set; }
        public string Type { get; set; } = string.Empty; // "file" | "link"
        public string? Url { get; set; }
        public string? Title { get; set; }
        public long? Size { get; set; }
        public string? MimeType { get; set; }
        public Guid? UploadedBy { get; set; }
        public DateTimeOffset CreatedAt { get; set; }

        public ProjectTask? Task { get; set; }
        public User? Uploader { get; set; }
    }
}
