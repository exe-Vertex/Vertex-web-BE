using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Vertex.Entities.Notifications;

namespace Vertex.Repositories.Interfaces
{
    public interface INotificationRepository
    {
        Task<List<Notification>> GetByUserIdAsync(Guid userId);
        Task AddAsync(Notification notification);
        Task MarkAsReadAsync(Guid id);
        Task MarkAllAsReadAsync(Guid userId);
    }
}


