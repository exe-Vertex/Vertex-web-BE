using System;
using System.Threading.Tasks;
using Vertex.Entities.Organizations;

namespace Vertex.Services.Interfaces
{
    public interface IInvitationService
    {
        Task<Invitation> CreateInvitationAsync(Guid creatorId, string email, string targetType, Guid targetId, string role);
        Task<Invitation> VerifyTokenAsync(string token);
        Task AcceptInvitationAsync(Guid userId, string token);
    }
}
