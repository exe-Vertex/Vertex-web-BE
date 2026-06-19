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

        public AuthController(IAuthService authService, AppDbContext context)
        {
            _authService = authService;
            _context = context;
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
                Role = me.Role
            });
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
