using System;
using System.ComponentModel.DataAnnotations;

namespace Vertex_web_BE.Models
{
    public class CreateInvitationRequest
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string TargetType { get; set; } = string.Empty; // "Project" or "Organization"

        [Required]
        public Guid TargetId { get; set; }

        [Required]
        public string Role { get; set; } = string.Empty;
    }

    public class AcceptInvitationRequest
    {
        [Required]
        public string Token { get; set; } = string.Empty;
    }
}
