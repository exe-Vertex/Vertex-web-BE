using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Vertex.Services.Models;

namespace Vertex.Services.Interfaces
{
    public interface ILecturerService
    {
        Task<List<LecturerGroupDto>> GetGroupsAsync(Guid lecturerId);
        Task<LecturerGroupDetailDto> GetGroupDetailAsync(Guid lecturerId, Guid projectId);
        Task ApproveTaskAsync(Guid lecturerId, Guid taskId);
        Task RequestChangesAsync(Guid lecturerId, Guid taskId);
        Task AddCommentAsync(Guid lecturerId, Guid taskId, string content);
        Task<List<NotificationDto>> GetNotificationsAsync(Guid userId);
        Task MarkNotificationReadAsync(Guid userId, Guid notificationId);
        Task MarkAllNotificationsReadAsync(Guid userId);
    }
}
