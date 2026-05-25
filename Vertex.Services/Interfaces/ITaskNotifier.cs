using System;
using System.Threading.Tasks;
using Vertex.Services.Models;

namespace Vertex.Services.Interfaces
{
    public interface ITaskNotifier
    {
        Task NotifyTaskCreatedAsync(Guid projectId, TaskDto task);
        Task NotifyTaskUpdatedAsync(Guid projectId, TaskDto task);
        Task NotifyTaskDeletedAsync(Guid projectId, Guid taskId);
    }
}
