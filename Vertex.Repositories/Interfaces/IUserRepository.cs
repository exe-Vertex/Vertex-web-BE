using System;
using System.Threading.Tasks;
using Vertex.Entities.Users;

namespace Vertex.Repositories.Interfaces
{
    public interface IUserRepository
    {
        Task<User?> GetByEmailAsync(string email);
        Task<User?> GetByExternalIdAsync(string provider, string externalId);
        Task<User?> GetByIdAsync(Guid id);
        Task AddAsync(User user);
        Task UpdateAsync(User user);
    }
}
