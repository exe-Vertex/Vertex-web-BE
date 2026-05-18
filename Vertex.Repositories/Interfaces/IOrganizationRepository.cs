using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Vertex.Entities.Organizations;

namespace Vertex.Repositories.Interfaces
{
    public interface IOrganizationRepository
    {
        Task<Organization?> GetByIdAsync(Guid id);
        Task<Organization?> GetByIdWithMembersAsync(Guid id);
        Task<Organization?> GetBySlugAsync(string slug);
        Task<List<Organization>> GetByUserIdAsync(Guid userId);
        Task AddAsync(Organization org);
        Task UpdateAsync(Organization org);

        Task<OrganizationMember?> GetMemberAsync(Guid orgId, Guid userId);
        Task<OrganizationMember?> GetMemberByIdAsync(Guid memberId);
        Task<List<OrganizationMember>> GetMembersAsync(Guid orgId);
        Task AddMemberAsync(OrganizationMember member);
        Task UpdateMemberAsync(OrganizationMember member);
        Task RemoveMemberAsync(OrganizationMember member);
        Task<int> CountMembersAsync(Guid orgId);
    }
}
