using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Vertex.Entities.Users;
using Vertex.Repositories;
using Vertex.Services.Interfaces;
using Vertex.Services.Models;
using Vertex_web_BE.Models;

namespace Vertex_web_BE.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _environment;

        public AuthController(IAuthService authService, AppDbContext context, IWebHostEnvironment environment)
        {
            _authService = authService;
            _context = context;
            _environment = environment;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            try
            {
                var tokens = await _authService.RegisterAsync(new RegisterInput(request.Name, request.Email, request.Password));
                return Ok(ToAuthResponse(tokens));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            try
            {
                var tokens = await _authService.LoginAsync(new LoginInput(request.Email, request.Password));
                return Ok(ToAuthResponse(tokens));
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
        }

        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
        {
            await _authService.ForgotPasswordAsync(request.Email);
            return Ok(new
            {
                message = "If an account with that email exists, a password reset link has been sent."
            });
        }

        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
        {
            try
            {
                await _authService.ResetPasswordAsync(request.Token, request.NewPassword);
                return Ok(new { message = "Password has been reset successfully." });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
        [HttpPost("external-login")]
        public async Task<IActionResult> ExternalLogin([FromBody] ExternalLoginRequest request)
        {
            try
            {
                var tokens = await _authService.ExternalLoginAsync(
                    new ExternalLoginInput(request.Provider, request.Token));
                return Ok(ToAuthResponse(tokens));
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [Authorize]
        [HttpGet("me")]
        public async Task<IActionResult> Me()
        {
            var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub);
            if (!Guid.TryParse(userIdValue, out var userId))
            {
                return Unauthorized(new { message = "Invalid access token." });
            }

            var me = await _authService.GetMeAsync(userId);
            return Ok(new MeResponse
            {
                Id = me.Id,
                Name = me.Name,
                Email = me.Email,
                Role = me.Role,
                AvatarUrl = me.AvatarUrl
            });
        }

        [Authorize]
        [HttpPost("avatar")]
        [RequestSizeLimit(1024 * 1024)]
        public async Task<IActionResult> UploadAvatar(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest(new { message = "Please select an image." });

            const long maxAvatarSize = 800 * 1024;
            if (file.Length > maxAvatarSize)
                return BadRequest(new { message = "Avatar must not exceed 800 KB." });

            var extension = file.ContentType.ToLowerInvariant() switch
            {
                "image/jpeg" => ".jpg",
                "image/png" => ".png",
                "image/gif" => ".gif",
                _ => null
            };
            if (extension == null || !await HasValidImageSignatureAsync(file, extension))
                return BadRequest(new { message = "Only valid JPG, PNG, or GIF images are supported." });

            var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub);
            if (!Guid.TryParse(userIdValue, out var userId))
                return Unauthorized(new { message = "Invalid access token." });

            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                return NotFound(new { message = "User not found." });

            DeleteStoredAvatar(user.AvatarUrl);

            var avatarsFolder = GetAvatarsFolder();
            Directory.CreateDirectory(avatarsFolder);
            var fileName = $"{user.Id:N}-{Guid.NewGuid():N}{extension}";
            var filePath = Path.Combine(avatarsFolder, fileName);

            await using (var target = System.IO.File.Create(filePath))
            {
                await file.CopyToAsync(target);
            }

            user.AvatarUrl = $"/uploads/avatars/{fileName}";
            user.UpdatedAt = DateTimeOffset.UtcNow;
            await _context.SaveChangesAsync();

            return Ok(new { avatarUrl = user.AvatarUrl });
        }

        [Authorize]
        [HttpDelete("avatar")]
        public async Task<IActionResult> RemoveAvatar()
        {
            var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub);
            if (!Guid.TryParse(userIdValue, out var userId))
                return Unauthorized(new { message = "Invalid access token." });

            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                return NotFound(new { message = "User not found." });

            DeleteStoredAvatar(user.AvatarUrl);
            user.AvatarUrl = string.Empty;
            user.UpdatedAt = DateTimeOffset.UtcNow;
            await _context.SaveChangesAsync();

            return NoContent();
        }
        [HttpPost("refresh")]
        public async Task<IActionResult> Refresh([FromBody] RefreshRequest request)
        {
            try
            {
                var tokens = await _authService.RefreshAsync(request.RefreshToken);
                return Ok(ToAuthResponse(tokens));
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
        }

        [HttpPost("logout")]
        public async Task<IActionResult> Logout([FromBody] RefreshRequest request)
        {
            await _authService.LogoutAsync(request.RefreshToken);
            return NoContent();
        }

        [Authorize]
        [HttpGet("skills")]
        public async Task<IActionResult> GetSkills()
        {
            var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub);
            if (!Guid.TryParse(userIdValue, out var userId))
            {
                return Unauthorized(new { message = "Invalid access token." });
            }

            var skills = await _context.UserSkills
                .Where(x => x.UserId == userId)
                .Select(x => x.SkillName)
                .ToListAsync();

            return Ok(skills);
        }

        [Authorize]
        [HttpPost("skills")]
        public async Task<IActionResult> UpdateSkills([FromBody] List<string> skills)
        {
            var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub);
            if (!Guid.TryParse(userIdValue, out var userId))
            {
                return Unauthorized(new { message = "Invalid access token." });
            }

            // Remove existing skills
            var existing = await _context.UserSkills.Where(x => x.UserId == userId).ToListAsync();
            _context.UserSkills.RemoveRange(existing);

            // Add new skills
            if (skills != null && skills.Count > 0)
            {
                foreach (var skill in skills)
                {
                    if (!string.IsNullOrWhiteSpace(skill))
                    {
                        _context.UserSkills.Add(new UserSkill
                        {
                            Id = Guid.NewGuid(),
                            UserId = userId,
                            SkillName = skill.Trim()
                        });
                    }
                }
            }

            await _context.SaveChangesAsync();
            return Ok(new { Message = "Skills updated successfully" });
        }

        private string GetAvatarsFolder()
        {
            var webRoot = string.IsNullOrWhiteSpace(_environment.WebRootPath)
                ? Path.Combine(_environment.ContentRootPath, "wwwroot")
                : _environment.WebRootPath;
            return Path.Combine(webRoot, "uploads", "avatars");
        }

        private void DeleteStoredAvatar(string? avatarUrl)
        {
            if (string.IsNullOrWhiteSpace(avatarUrl)
                || !avatarUrl.StartsWith("/uploads/avatars/", StringComparison.OrdinalIgnoreCase))
                return;

            var fileName = Path.GetFileName(avatarUrl);
            var filePath = Path.Combine(GetAvatarsFolder(), fileName);
            if (System.IO.File.Exists(filePath))
                System.IO.File.Delete(filePath);
        }

        private static async Task<bool> HasValidImageSignatureAsync(IFormFile file, string extension)
        {
            var header = new byte[8];
            await using var stream = file.OpenReadStream();
            var bytesRead = await stream.ReadAsync(header.AsMemory(0, header.Length));

            return extension switch
            {
                ".jpg" => bytesRead >= 3
                    && header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF,
                ".png" => bytesRead >= 8
                    && header.SequenceEqual(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }),
                ".gif" => bytesRead >= 6
                    && (System.Text.Encoding.ASCII.GetString(header, 0, 6) == "GIF87a"
                        || System.Text.Encoding.ASCII.GetString(header, 0, 6) == "GIF89a"),
                _ => false
            };
        }
        private static AuthResponse ToAuthResponse(AuthTokens tokens)
        {
            return new AuthResponse
            {
                AccessToken = tokens.AccessToken,
                AccessTokenExpiresAt = tokens.AccessTokenExpiresAt,
                RefreshToken = tokens.RefreshToken,
                RefreshTokenExpiresAt = tokens.RefreshTokenExpiresAt
            };
        }
    }
}
