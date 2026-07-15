using Learna.Core.Entities;

namespace Learna.Core.Interfaces;

public interface IUserRepository
{
    Task<IEnumerable<User>> GetAllAsync(string? search = null);
    Task<User?> GetByIdAsync(int id);
    Task<User?> GetByEmailAsync(string email);
    Task<User> CreateAsync(User user);
    Task<User> UpdateAsync(User user);
    Task<bool> EmailExistsAsync(string email);
    Task<IEnumerable<string>> GetUserRolesAsync(int userId);
    Task<Role?> GetRoleByNameAsync(string roleName);
}
