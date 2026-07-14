using Learna.Core.Entities;

namespace Learna.Core.Interfaces;

public interface ISubjectRepository
{
    Task<IEnumerable<Subject>> GetAllAsync();
    Task<Subject?> GetByIdAsync(int id);
    Task<Subject> CreateAsync(Subject subject);
    Task<Subject> UpdateAsync(Subject subject);
    Task<bool> DeleteAsync(int id);
    Task<bool> CodeExistsAsync(string code);
}
