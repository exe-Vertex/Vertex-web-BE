using System;
using System.Threading.Tasks;
using Vertex.Services.Models;

namespace Vertex.Services.Interfaces
{
    public interface IAuthService
    {
        Task<AuthTokens> RegisterAsync(RegisterInput input);
        Task<AuthTokens> LoginAsync(LoginInput input);
        Task<AuthUser> GetMeAsync(Guid userId);
        Task<AuthTokens> RefreshAsync(string refreshToken);
        Task LogoutAsync(string refreshToken);
    }
}
