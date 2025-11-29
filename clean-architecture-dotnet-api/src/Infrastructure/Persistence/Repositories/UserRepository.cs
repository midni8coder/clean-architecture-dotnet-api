namespace Infrastructure.Persistence.Repositories;

using Microsoft.EntityFrameworkCore;
using Domain.Entities;
using Domain.Interfaces;

/// <summary>
/// User repository with specialized queries.
/// </summary>
public class UserRepository : Repository<User>, IUserRepository
{
    public UserRepository(CleanArchDbContext context) : base(context)
    {
    }

    public async Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .FirstOrDefaultAsync(u => u.Email == email, cancellationToken);
    }

    public async Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AnyAsync(u => u.Email == email, cancellationToken);
    }
}
