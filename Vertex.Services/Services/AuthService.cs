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

        public AuthService(
            IUserRepository userRepository,
            IRefreshTokenRepository refreshTokenRepository,
            ITokenService tokenService)
        {
            _userRepository = userRepository;
            _refreshTokenRepository = refreshTokenRepository;
            _tokenService = tokenService;
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
                CreatedAt = now,
                UpdatedAt = now
            };

            await _userRepository.AddAsync(user);

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
