using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Vertex.Entities.Users;
using Vertex.Repositories.Interfaces;

namespace Vertex.Repositories.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly AppDbContext _dbContext;

        public UserRepository(AppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public Task<User?> GetByEmailAsync(string email)
        {
            return _dbContext.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Email == email);
        }

        public Task<User?> GetByIdAsync(Guid id)
        {
            return _dbContext.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task AddAsync(User user)
        {
            _dbContext.Users.Add(user);
            await _dbContext.SaveChangesAsync();
        }

        public async Task UpdateAsync(User user)
        {
            _dbContext.Users.Update(user);
            await _dbContext.SaveChangesAsync();
        }
    }
}
