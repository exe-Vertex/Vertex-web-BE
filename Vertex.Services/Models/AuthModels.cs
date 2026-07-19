using System;

namespace Vertex.Services.Models
{
    public record AuthTokens(
        string AccessToken,
        DateTimeOffset AccessTokenExpiresAt,
        string RefreshToken,
        DateTimeOffset RefreshTokenExpiresAt
    );

    public record AuthUser(Guid Id, string Name, string Email, string Role);

    public record RegisterInput(string Name, string Email, string Password);

    public record LoginInput(string Email, string Password);

    public record TokenGenerationResult(AuthTokens Tokens, string RefreshTokenHash);

    public record ExternalLoginInput(string Provider, string Token);

    public record ExternalUserInfo(string ExternalId, string Email, string Name, string? AvatarUrl);
}
