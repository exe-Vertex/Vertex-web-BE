using System;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using BCrypt.Net;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Vertex.Entities.Auth;
using Vertex.Entities.Users;
using Vertex.Repositories;
using Vertex.Repositories.Interfaces;
using Vertex.Services.Interfaces;
using Vertex.Services.Models;

namespace Vertex.Services.Services
{
    public class AuthService : IAuthService
    {
        private readonly IUserRepository _userRepository;
        private readonly IRefreshTokenRepository _refreshTokenRepository;
        private readonly ITokenService _tokenService;
        private readonly IOrganizationService _orgService;
        private readonly IExternalAuthProvider _externalAuthProvider;
        private readonly AppDbContext _dbContext;
        private readonly IEmailService _emailService;
        private readonly PasswordResetOptions _passwordResetOptions;
        private readonly ILogger<AuthService> _logger;

        public AuthService(
            IUserRepository userRepository,
            IRefreshTokenRepository refreshTokenRepository,
            ITokenService tokenService,
            IOrganizationService orgService,
            IExternalAuthProvider externalAuthProvider,
            AppDbContext dbContext,
            IEmailService emailService,
            IOptions<PasswordResetOptions> passwordResetOptions,
            ILogger<AuthService> logger)
        {
            _userRepository = userRepository;
            _refreshTokenRepository = refreshTokenRepository;
            _tokenService = tokenService;
            _orgService = orgService;
            _externalAuthProvider = externalAuthProvider;
            _dbContext = dbContext;
            _emailService = emailService;
            _passwordResetOptions = passwordResetOptions.Value;
            _logger = logger;
        }

        public async Task<AuthTokens> RegisterAsync(RegisterInput input)
        {
            var existing = await _userRepository.GetByEmailAsync(input.Email);
            if (existing != null)
            {
                throw new InvalidOperationException("Email already exists.");
            }

            var now = DateTimeOffset.UtcNow;
            var user = new User
            {
                Id = Guid.NewGuid(),
                Name = input.Name,
                Email = input.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(input.Password),
                AuthProvider = "local",
                CreatedAt = now,
                UpdatedAt = now
            };

            await _userRepository.AddAsync(user);

            // Auto-create a default organization for the new user
            var orgSlug = user.Name.ToLower().Trim().Replace(" ", "-") + "-workspace";
            await _orgService.CreateOrgAsync(user.Id, new CreateOrgInput($"{user.Name}'s Workspace", orgSlug));

            var result = _tokenService.GenerateTokens(user);
            await SaveRefreshTokenAsync(user.Id, result.Tokens, result.RefreshTokenHash);

            return result.Tokens;
        }

        public async Task<AuthTokens> LoginAsync(LoginInput input)
        {
            var user = await _userRepository.GetByEmailAsync(input.Email);
            if (user == null ||
                string.IsNullOrWhiteSpace(user.PasswordHash) ||
                !BCrypt.Net.BCrypt.Verify(input.Password, user.PasswordHash))
            {
                throw new UnauthorizedAccessException("Invalid email or password.");
            }

            var result = _tokenService.GenerateTokens(user);
            await SaveRefreshTokenAsync(user.Id, result.Tokens, result.RefreshTokenHash);

            return result.Tokens;
        }

        public async Task<AuthTokens> ExternalLoginAsync(ExternalLoginInput input)
        {
            var provider = input.Provider.ToLowerInvariant();

            // 1. Verify token with external provider → get user info
            var externalInfo = await _externalAuthProvider.VerifyTokenAsync(provider, input.Token);

            // 2. Try to find existing user by ExternalId + Provider
            var user = await _userRepository.GetByExternalIdAsync(provider, externalInfo.ExternalId);

            if (user == null)
            {
                // 3. Try to find by email (link account if exists)
                user = await _userRepository.GetByEmailAsync(externalInfo.Email);

                if (user != null)
                {
                    // Link existing account with this OAuth provider
                    user.AuthProvider = provider;
                    user.ExternalId = externalInfo.ExternalId;
                    if (!string.IsNullOrEmpty(externalInfo.AvatarUrl) && string.IsNullOrEmpty(user.AvatarUrl))
                    {
                        user.AvatarUrl = externalInfo.AvatarUrl;
                    }
                    user.UpdatedAt = DateTimeOffset.UtcNow;
                    await _userRepository.UpdateAsync(user);
                }
                else
                {
                    // 4. Create new user (no password)
                    var now = DateTimeOffset.UtcNow;
                    user = new User
                    {
                        Id = Guid.NewGuid(),
                        Name = externalInfo.Name,
                        Email = externalInfo.Email,
                        PasswordHash = string.Empty,
                        AuthProvider = provider,
                        ExternalId = externalInfo.ExternalId,
                        AvatarUrl = externalInfo.AvatarUrl ?? string.Empty,
                        CreatedAt = now,
                        UpdatedAt = now
                    };

                    await _userRepository.AddAsync(user);

                    // Auto-create a default organization for the new user
                    var orgSlug = user.Name.ToLower().Trim().Replace(" ", "-") + "-workspace";
                    await _orgService.CreateOrgAsync(user.Id, new CreateOrgInput($"{user.Name}'s Workspace", orgSlug));
                }
            }

            var result = _tokenService.GenerateTokens(user);
            await SaveRefreshTokenAsync(user.Id, result.Tokens, result.RefreshTokenHash);

            return result.Tokens;
        }

        public async Task<AuthUser> GetMeAsync(Guid userId)
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
            {
                throw new InvalidOperationException("User not found.");
            }

            return new AuthUser(user.Id, user.Name, user.Email, user.Role);
        }

        public async Task<AuthTokens> RefreshAsync(string refreshToken)
        {
            var tokenHash = _tokenService.HashRefreshToken(refreshToken);
            var token = await _refreshTokenRepository.GetByTokenHashAsync(tokenHash);

            if (token == null || token.RevokedAt.HasValue || token.ExpiresAt <= DateTimeOffset.UtcNow)
            {
                throw new UnauthorizedAccessException("Invalid refresh token.");
            }

            var user = await _userRepository.GetByIdAsync(token.UserId);
            if (user == null)
            {
                throw new UnauthorizedAccessException("Invalid refresh token.");
            }

            token.RevokedAt = DateTimeOffset.UtcNow;

            var result = _tokenService.GenerateTokens(user);
            token.ReplacedByTokenHash = result.RefreshTokenHash;
            await _refreshTokenRepository.UpdateAsync(token);

            await SaveRefreshTokenAsync(user.Id, result.Tokens, result.RefreshTokenHash);

            return result.Tokens;
        }

        public async Task LogoutAsync(string refreshToken)
        {
            var tokenHash = _tokenService.HashRefreshToken(refreshToken);
            var token = await _refreshTokenRepository.GetByTokenHashAsync(tokenHash);
            if (token == null || token.RevokedAt.HasValue)
            {
                return;
            }

            token.RevokedAt = DateTimeOffset.UtcNow;
            await _refreshTokenRepository.UpdateAsync(token);
        }

        public async Task ForgotPasswordAsync(string email)
        {
            var normalizedEmail = email.Trim().ToLowerInvariant();
            var user = await _dbContext.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Email.ToLower() == normalizedEmail);

            // Keep the response identical for unknown and external-only accounts.
            if (user == null || string.IsNullOrWhiteSpace(user.PasswordHash))
            {
                return;
            }

            var now = DateTimeOffset.UtcNow;
            var tokenLifetimeMinutes = Math.Clamp(_passwordResetOptions.TokenLifetimeMinutes, 5, 60);
            var recentlyRequested = await _dbContext.PasswordResetTokens
                .AsNoTracking()
                .AnyAsync(x => x.UserId == user.Id && x.CreatedAt > now.AddMinutes(-2));

            if (recentlyRequested)
            {
                return;
            }

            await _dbContext.PasswordResetTokens
                .Where(x => x.UserId == user.Id)
                .ExecuteDeleteAsync();

            var rawToken = GenerateResetToken();
            var resetToken = new PasswordResetToken
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                TokenHash = HashResetToken(rawToken),
                CreatedAt = now,
                ExpiresAt = now.AddMinutes(tokenLifetimeMinutes)
            };

            _dbContext.PasswordResetTokens.Add(resetToken);
            await _dbContext.SaveChangesAsync();

            var frontendUrl = _passwordResetOptions.FrontendUrl.TrimEnd('/');
            var resetUrl = $"{frontendUrl}/#/reset-password?token={Uri.EscapeDataString(rawToken)}";
            var safeName = WebUtility.HtmlEncode(user.Name);
            var safeResetUrl = WebUtility.HtmlEncode(resetUrl);
            var body = $"<p>Xin chào {safeName},</p><p>Bạn vừa yêu cầu đặt lại mật khẩu Vertex.</p><p><a href='{safeResetUrl}'>Đặt lại mật khẩu</a></p><p>Liên kết này hết hạn sau {tokenLifetimeMinutes} phút và chỉ dùng được một lần.</p><p>Nếu bạn không yêu cầu, hãy bỏ qua email này.</p>";

            try
            {
                await _emailService.SendEmailAsync(user.Email, "Vertex - Đặt lại mật khẩu", body);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send password reset email for user {UserId}.", user.Id);

                try
                {
                    _dbContext.PasswordResetTokens.Remove(resetToken);
                    await _dbContext.SaveChangesAsync();
                }
                catch (Exception cleanupException)
                {
                    _logger.LogError(cleanupException, "Failed to remove unusable password reset token {TokenId}.", resetToken.Id);
                }
            }
        }

        public async Task ResetPasswordAsync(string token, string newPassword)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                throw new InvalidOperationException("Reset link is invalid or has expired.");
            }

            if (newPassword.Length is < 8 or > 128 ||
                !newPassword.Any(char.IsLetter) ||
                !newPassword.Any(char.IsDigit))
            {
                throw new InvalidOperationException("Password must contain 8 to 128 characters, including a letter and a number.");
            }

            var tokenHash = HashResetToken(token);
            var now = DateTimeOffset.UtcNow;

            await using var transaction = await _dbContext.Database.BeginTransactionAsync();

            var resetToken = await _dbContext.PasswordResetTokens
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.TokenHash == tokenHash);

            if (resetToken == null || resetToken.UsedAt.HasValue || resetToken.ExpiresAt <= now)
            {
                throw new InvalidOperationException("Reset link is invalid or has expired.");
            }

            var consumed = await _dbContext.PasswordResetTokens
                .Where(x => x.Id == resetToken.Id && x.UsedAt == null && x.ExpiresAt > now)
                .ExecuteUpdateAsync(setters => setters.SetProperty(x => x.UsedAt, now));

            if (consumed != 1)
            {
                throw new InvalidOperationException("Reset link is invalid or has expired.");
            }

            var user = await _dbContext.Users.FirstOrDefaultAsync(x => x.Id == resetToken.UserId);
            if (user == null || string.IsNullOrWhiteSpace(user.PasswordHash))
            {
                throw new InvalidOperationException("Reset link is invalid or has expired.");
            }

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
            user.UpdatedAt = now;

            await _dbContext.RefreshTokens
                .Where(x => x.UserId == user.Id && x.RevokedAt == null)
                .ExecuteUpdateAsync(setters => setters.SetProperty(x => x.RevokedAt, now));

            await _dbContext.SaveChangesAsync();
            await transaction.CommitAsync();
        }

        private static string GenerateResetToken()
        {
            return Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
                .TrimEnd('=')
                .Replace('+', '-')
                .Replace('/', '_');
        }

        private static string HashResetToken(string token)
        {
            return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
        }

        private Task SaveRefreshTokenAsync(Guid userId, AuthTokens tokens, string refreshTokenHash)
        {
            var refreshToken = new RefreshToken
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                TokenHash = refreshTokenHash,
                ExpiresAt = tokens.RefreshTokenExpiresAt,
                CreatedAt = DateTimeOffset.UtcNow
            };

            return _refreshTokenRepository.AddAsync(refreshToken);
        }
    }
}
