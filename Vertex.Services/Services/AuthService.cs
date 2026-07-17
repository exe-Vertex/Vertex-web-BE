using System;
using System.Threading.Tasks;
using BCrypt.Net;
using Vertex.Entities.Auth;
using Vertex.Entities.Users;
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

        public AuthService(
            IUserRepository userRepository,
            IRefreshTokenRepository refreshTokenRepository,
            ITokenService tokenService,
            IOrganizationService orgService,
            IExternalAuthProvider externalAuthProvider)
        {
            _userRepository = userRepository;
            _refreshTokenRepository = refreshTokenRepository;
            _tokenService = tokenService;
            _orgService = orgService;
            _externalAuthProvider = externalAuthProvider;
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
            if (user == null || !BCrypt.Net.BCrypt.Verify(input.Password, user.PasswordHash))
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
