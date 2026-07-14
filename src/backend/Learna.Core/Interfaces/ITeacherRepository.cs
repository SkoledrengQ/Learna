using Learna.Core.Entities;

namespace Learna.Core.Interfaces;

public interface ITeacherRepository
{
    Task<IEnumerable<Teacher>> GetAllAsync();
    Task<Teacher?> GetByIdAsync(int id);
    Task<Teacher> CreateAsync(Teacher teacher);
    Task<Teacher> UpdateAsync(Teacher teacher);
    Task<bool> DeleteAsync(int id);
    Task<bool> EmailExistsAsync(string email);
}
