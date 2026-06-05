using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Vertex.Entities.AI;

namespace Vertex.Repositories.Interfaces
{
    public interface IAiHistoryRepository
    {
        Task<AiHistory> AddAsync(AiHistory history);
        Task<List<AiHistory>> GetByUserIdAsync(Guid userId);
    }
}
