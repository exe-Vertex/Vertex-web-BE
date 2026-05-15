using System.Threading.Tasks;
using Vertex.Entities.Auth;

namespace Vertex.Repositories.Interfaces
{
    public interface IRefreshTokenRepository
    {
        Task AddAsync(RefreshToken token);
        Task<RefreshToken?> GetByTokenHashAsync(string tokenHash);
        Task UpdateAsync(RefreshToken token);
    }
}
