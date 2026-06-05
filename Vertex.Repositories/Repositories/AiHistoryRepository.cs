using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Vertex.Entities.AI;
using Vertex.Repositories.Interfaces;

namespace Vertex.Repositories.Repositories
{
    public class AiHistoryRepository : IAiHistoryRepository
    {
        private readonly AppDbContext _context;

        public AiHistoryRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<AiHistory> AddAsync(AiHistory history)
        {
            await _context.AiHistories.AddAsync(history);
            await _context.SaveChangesAsync();
            return history;
        }

        public async Task<List<AiHistory>> GetByUserIdAsync(Guid userId)
        {
            return await _context.AiHistories
                .Where(x => x.UserId == userId)
                .OrderByDescending(x => x.CreatedAt)
                .ToListAsync();
        }
    }
}
