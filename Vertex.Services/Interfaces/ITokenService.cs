using Vertex.Entities.Users;
using Vertex.Services.Models;

namespace Vertex.Services.Interfaces
{
    public interface ITokenService
    {
        TokenGenerationResult GenerateTokens(User user);
        string HashRefreshToken(string refreshToken);
    }
}
