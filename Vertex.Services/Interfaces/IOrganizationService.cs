using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Vertex.Services.Models;

namespace Vertex.Services.Interfaces
{
    public interface IOrganizationService
    {
        Task<OrgSummary> CreateOrgAsync(Guid ownerId, CreateOrgInput input);
        Task<List<OrgSummary>> GetMyOrgsAsync(Guid userId);
        Task<OrgDetail> GetOrgDetailAsync(Guid orgId, Guid requesterId);
        Task<OrgMemberDto> InviteMemberAsync(Guid orgId, Guid requesterId, InviteMemberInput input);
        Task UpdateMemberRoleAsync(Guid orgId, Guid memberId, Guid requesterId, UpdateMemberRoleInput input);
        Task RemoveMemberAsync(Guid orgId, Guid memberId, Guid requesterId);
    }
}
