using System;
using Vertex.Entities.Users;

namespace Vertex.Entities.Projects
{
    public class ProjectLink
    {
        public Guid Id { get; set; }
        public Guid ProjectId { get; set; }
        public string Url { get; set; }
        public string Title { get; set; }
        public Guid? UploadedBy { get; set; }
        public DateTimeOffset CreatedAt { get; set; }

        public Project? Project { get; set; }
        public User? Uploader { get; set; }
    }
}
