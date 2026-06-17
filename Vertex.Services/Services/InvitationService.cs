using Microsoft.EntityFrameworkCore;
using System;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Vertex.Entities.Organizations;
using Vertex.Entities.Projects;
using Vertex.Repositories;
using Vertex.Services.Interfaces;

namespace Vertex.Services.Services
{
    public class InvitationService : IInvitationService
    {
        private readonly AppDbContext _context;
        private readonly IEmailService _emailService;

        public InvitationService(AppDbContext context, IEmailService emailService)
        {
            _context = context;
            _emailService = emailService;
        }

        public async Task<Invitation> CreateInvitationAsync(Guid creatorId, string email, string targetType, Guid targetId, string role)
        {
            // Kiểm tra xem đã có invitation Pending chưa
            var existing = await _context.Invitations
                .FirstOrDefaultAsync(x => x.Email == email && x.TargetId == targetId && x.Status == "Pending");
            
            if (existing != null)
            {
                existing.Status = "Revoked"; // Thu hồi cái cũ
            }

            var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
                .Replace("+", "-").Replace("/", "_").TrimEnd('='); // URL safe token

            var invitation = new Invitation
            {
                Id = Guid.NewGuid(),
                Email = email,
                TargetType = targetType,
                TargetId = targetId,
                Role = role,
                Token = token,
                Status = "Pending",
                CreatedBy = creatorId,
                CreatedAt = DateTimeOffset.UtcNow,
                ExpiredAt = DateTimeOffset.UtcNow.AddDays(7)
            };

            _context.Invitations.Add(invitation);
            await _context.SaveChangesAsync();

            // Gửi email
            var acceptLink = $"http://localhost:3000/#/invite/accept?token={token}";
            var subject = $"You have been invited to join a {targetType} on Vertex";
            var body = $@"
                <h3>Hello,</h3>
                <p>You have been invited to join a {targetType} on Vertex with the role of <strong>{role}</strong>.</p>
                <p>Please click the link below to accept the invitation:</p>
                <p><a href='{acceptLink}' style='padding: 10px 20px; background-color: #22c55e; color: white; text-decoration: none; border-radius: 5px;'>Accept Invitation</a></p>
                <p>If you don't have an account yet, you will be asked to sign up first.</p>
                <p>This link will expire in 7 days.</p>
                <br/>
                <p>Regards,<br/>Vertex Team</p>
            ";

            await _emailService.SendEmailAsync(email, subject, body);

            return invitation;
        }

        public async Task<Invitation> VerifyTokenAsync(string token)
        {
            var invitation = await _context.Invitations.FirstOrDefaultAsync(x => x.Token == token);
            if (invitation == null)
            {
                throw new InvalidOperationException("Invalid invitation token.");
            }

            if (invitation.Status != "Pending")
            {
                throw new InvalidOperationException($"This invitation is {invitation.Status.ToLower()}.");
            }

            if (invitation.ExpiredAt < DateTimeOffset.UtcNow)
            {
                invitation.Status = "Expired";
                await _context.SaveChangesAsync();
                throw new InvalidOperationException("This invitation has expired.");
            }

            return invitation;
        }

        public async Task AcceptInvitationAsync(Guid userId, string token)
        {
            var invitation = await VerifyTokenAsync(token);
            
            var user = await _context.Users.FindAsync(userId);
            if (user == null || user.Email != invitation.Email)
            {
                throw new InvalidOperationException("You must be logged in with the invited email address to accept this invitation.");
            }

            if (invitation.TargetType == "Organization")
            {
                // Add to Org
                var existingMember = await _context.OrganizationMembers.FirstOrDefaultAsync(x => x.OrgId == invitation.TargetId && x.UserId == userId);
                if (existingMember == null)
                {
                    var newMember = new OrganizationMember
                    {
                        Id = Guid.NewGuid(),
                        OrgId = invitation.TargetId,
                        UserId = userId,
                        Role = invitation.Role,
                        JoinedAt = DateTimeOffset.UtcNow
                    };
                    _context.OrganizationMembers.Add(newMember);
                }
            }
            else if (invitation.TargetType == "Project")
            {
                // Add to Project
                var existingMember = await _context.ProjectMembers.FirstOrDefaultAsync(x => x.ProjectId == invitation.TargetId && x.UserId == userId);
                if (existingMember == null)
                {
                    var newMember = new ProjectMember
                    {
                        Id = Guid.NewGuid(),
                        ProjectId = invitation.TargetId,
                        UserId = userId,
                        Role = invitation.Role,
                        JoinedAt = DateTimeOffset.UtcNow
                    };
                    _context.ProjectMembers.Add(newMember);
                }

                // Tự động thêm vào Organization của Project để người dùng có thể thấy Organization và Project trên Dashboard
                var project = await _context.Projects.FindAsync(invitation.TargetId);
                if (project != null)
                {
                    var existingOrgMember = await _context.OrganizationMembers
                        .FirstOrDefaultAsync(x => x.OrgId == project.OrgId && x.UserId == userId);
                    if (existingOrgMember == null)
                    {
                        var newOrgMember = new OrganizationMember
                        {
                            Id = Guid.NewGuid(),
                            OrgId = project.OrgId,
                            UserId = userId,
                            Role = "member", // Role mặc định trong tổ chức
                            JoinedAt = DateTimeOffset.UtcNow
                        };
                        _context.OrganizationMembers.Add(newOrgMember);
                    }
                }
            }

            invitation.Status = "Accepted";
            await _context.SaveChangesAsync();
        }
    }
}
