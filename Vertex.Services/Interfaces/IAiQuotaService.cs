using System;
using System.Threading.Tasks;

namespace Vertex.Services.Interfaces
{
    public interface IAiQuotaService
    {
        Task<DateTimeOffset> ConsumeAsync(Guid userId, Guid orgId);
        Task RefundAsync(Guid orgId, DateTimeOffset periodStart);
    }
}