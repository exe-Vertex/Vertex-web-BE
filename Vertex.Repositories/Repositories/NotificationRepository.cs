using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Vertex.Entities.Notifications;
using Vertex.Repositories.Interfaces;

namespace Vertex.Repositories.Repositories
{
    public class NotificationRepository : INotificationRepository
    {
        private readonly AppDbContext _dbContext;

        public NotificationRepository(AppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public Task<List<Notification>> GetByUserIdAsync(Guid userId)
        {
            return _dbContext.Notifications
                .AsNoTracking()
                .Where(x => x.UserId == userId)
                .OrderByDescending(x => x.CreatedAt)
                .ToListAsync();
        }

        public async Task MarkAsReadAsync(Guid id)
        {
            var notification = await _dbContext.Notifications.FindAsync(id);
            if (notification != null)
            {
                notification.IsRead = true;
                await _dbContext.SaveChangesAsync();
            }
        }

        public async Task MarkAllAsReadAsync(Guid userId)
        {
            await _dbContext.Notifications
                .Where(x => x.UserId == userId && !x.IsRead)
                .ExecuteUpdateAsync(x => x.SetProperty(n => n.IsRead, true));
        }
    }
}
