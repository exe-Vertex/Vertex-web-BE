using Microsoft.AspNetCore.SignalR;
using System;
using System.Threading.Tasks;
using Vertex.Services.Interfaces;
using Vertex.Services.Models;
using Vertex_web_BE.Hubs;

namespace Vertex_web_BE.Services
{
    public class SignalRTaskNotifier : ITaskNotifier
    {
        private readonly IHubContext<TaskHub> _hubContext;

        public SignalRTaskNotifier(IHubContext<TaskHub> hubContext)
        {
            _hubContext = hubContext;
        }

        public async Task NotifyTaskCreatedAsync(Guid projectId, TaskDto task)
        {
            await _hubContext.Clients.Group($"Project_{projectId}").SendAsync("TaskCreated", task);
        }

        public async Task NotifyTaskUpdatedAsync(Guid projectId, TaskDto task)
        {
            await _hubContext.Clients.Group($"Project_{projectId}").SendAsync("TaskUpdated", task);
        }

        public async Task NotifyTaskDeletedAsync(Guid projectId, Guid taskId)
        {
            await _hubContext.Clients.Group($"Project_{projectId}").SendAsync("TaskDeleted", taskId);
        }
    }
}
