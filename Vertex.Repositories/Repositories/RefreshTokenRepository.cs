using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Vertex.Entities.Auth;
using Vertex.Repositories.Interfaces;

namespace Vertex.Repositories.Repositories
{
    public class RefreshTokenRepository : IRefreshTokenRepository
    {
        private readonly AppDbContext _dbContext;

        public RefreshTokenRepository(AppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task AddAsync(RefreshToken token)
        {
            _dbContext.RefreshTokens.Add(token);
            await _dbContext.SaveChangesAsync();
        }

        public Task<RefreshToken?> GetByTokenHashAsync(string tokenHash)
        {
            return _dbContext.RefreshTokens.FirstOrDefaultAsync(x => x.TokenHash == tokenHash);
        }

        public async Task UpdateAsync(RefreshToken token)
        {
            _dbContext.RefreshTokens.Update(token);
            await _dbContext.SaveChangesAsync();
        }
    }
}
