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

        public async Task<DateTimeOffset> ConsumeAsync(Guid userId, Guid orgId)
        {
            if (orgId == Guid.Empty)
                throw new InvalidOperationException("Organization is required.");

            var now = DateTimeOffset.UtcNow;
            var periodStart = new DateTimeOffset(now.Year, now.Month, 1, 0, 0, 0, TimeSpan.Zero);

            var userState = await _dbContext.Users
                .AsNoTracking()
                .Where(user => user.Id == userId)
                .Select(user => new
                {
                    user.Status,
                    user.Role,
                    HasOrganizationAccess = user.OrganizationMemberships.Any(member => member.OrgId == orgId)
                })
                .FirstOrDefaultAsync();

            if (userState == null)
                throw new KeyNotFoundException("User not found.");

            if (!string.Equals(userState.Status, "active", StringComparison.OrdinalIgnoreCase))
                throw new UnauthorizedAccessException("This account is not active.");

            if (!userState.HasOrganizationAccess &&
                !string.Equals(userState.Role, "admin", StringComparison.OrdinalIgnoreCase))
            {
                throw new UnauthorizedAccessException("You do not have access to this organization.");
            }

            await _dbContext.Organizations
                .Where(org => org.Id == orgId && org.AiQuotaPeriodStart < periodStart)
                .ExecuteUpdateAsync(update => update
                    .SetProperty(org => org.AiUsed, 0)
                    .SetProperty(org => org.AiQuotaPeriodStart, periodStart)
                    .SetProperty(org => org.UpdatedAt, now));

            var affectedRows = await _dbContext.Organizations
                .Where(org => org.Id == orgId && org.AiUsed < org.AiQuota)
                .ExecuteUpdateAsync(update => update
                    .SetProperty(org => org.AiUsed, org => org.AiUsed + 1)
                    .SetProperty(org => org.UpdatedAt, now));

            if (affectedRows == 1)
                return periodStart;

            var orgState = await _dbContext.Organizations
                .AsNoTracking()
                .Where(org => org.Id == orgId)
                .Select(org => new { org.AiQuota, org.AiUsed })
                .FirstOrDefaultAsync();

            if (orgState == null)
                throw new KeyNotFoundException("Organization not found.");

            throw new AiQuotaExceededException(orgState.AiQuota, orgState.AiUsed);
        }

        public async Task RefundAsync(Guid orgId, DateTimeOffset periodStart)
        {
            var now = DateTimeOffset.UtcNow;
            await _dbContext.Organizations
                .Where(org =>
                    org.Id == orgId &&
                    org.AiQuotaPeriodStart == periodStart &&
                    org.AiUsed > 0)
                .ExecuteUpdateAsync(update => update
                    .SetProperty(org => org.AiUsed, org => org.AiUsed - 1)
                    .SetProperty(org => org.UpdatedAt, now));
        }
    }
}