using System;
using System.Threading.Tasks;

namespace Vertex.Services.Interfaces
{
    public interface IStorageUsageService
    {
        Task<long> GetUsedBytesAsync(Guid orgId);
        Task EnsureCanStoreAsync(Guid orgId, long additionalBytes);
    }
}
