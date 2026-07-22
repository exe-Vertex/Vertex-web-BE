using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Vertex.Repositories;
using Vertex.Services.Exceptions;
using Vertex.Services.Interfaces;

namespace Vertex.Services.Services
{
    public class AiQuotaService : IAiQuotaService
    {
        private readonly AppDbContext _dbContext;

        public AiQuotaService(AppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task ConsumeAsync(Guid userId)
        {
            var now = DateTimeOffset.UtcNow;
            var affectedRows = await _dbContext.Users
                .Where(user =>
                    user.Id == userId &&
                    user.Status == "active" &&
                    user.AiUsed < user.AiQuota)
                .ExecuteUpdateAsync(update => update
                    .SetProperty(user => user.AiUsed, user => user.AiUsed + 1)
                    .SetProperty(user => user.UpdatedAt, now));

            if (affectedRows == 1)
                return;

            var userState = await _dbContext.Users
                .AsNoTracking()
                .Where(user => user.Id == userId)
                .Select(user => new { user.Status, user.AiQuota, user.AiUsed })
                .FirstOrDefaultAsync();

            if (userState == null)
                throw new KeyNotFoundException("User not found.");

            if (!string.Equals(userState.Status, "active", StringComparison.OrdinalIgnoreCase))
                throw new UnauthorizedAccessException("This account is not active.");

            throw new AiQuotaExceededException(userState.AiQuota, userState.AiUsed);
        }

        public async Task RefundAsync(Guid userId)
        {
            var now = DateTimeOffset.UtcNow;
            await _dbContext.Users
                .Where(user => user.Id == userId && user.AiUsed > 0)
                .ExecuteUpdateAsync(update => update
                    .SetProperty(user => user.AiUsed, user => user.AiUsed - 1)
                    .SetProperty(user => user.UpdatedAt, now));
        }
    }
}
