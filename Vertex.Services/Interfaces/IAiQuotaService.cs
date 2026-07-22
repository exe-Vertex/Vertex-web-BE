using System;
using System.Threading.Tasks;

namespace Vertex.Services.Interfaces
{
    public interface IAiQuotaService
    {
        Task ConsumeAsync(Guid userId);
        Task RefundAsync(Guid userId);
    }
}
