using System;

namespace Vertex.Entities.Users
{
    public class UserSkill
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public string SkillName { get; set; } = string.Empty;

        // Navigation
        public User User { get; set; } = null!;
    }
}
