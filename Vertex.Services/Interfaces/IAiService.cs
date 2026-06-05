using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Vertex.Entities.AI;

namespace Vertex.Services.Interfaces
{
    public interface IAiService
    {
        Task<AiHistory> ChatAsync(Guid userId, string prompt);
        Task<List<AiHistory>> GetHistoryAsync(Guid userId);
    }
}
