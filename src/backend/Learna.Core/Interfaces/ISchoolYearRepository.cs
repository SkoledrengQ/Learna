using Learna.Core.Entities;

namespace Learna.Core.Interfaces;

public interface ISchoolYearRepository
{
    Task<IEnumerable<SchoolYear>> GetAllAsync();
    Task<SchoolYear?> GetByIdAsync(int id);
    Task<SchoolYear> CreateAsync(SchoolYear schoolYear);
    Task<SchoolYear> UpdateAsync(SchoolYear schoolYear);
    Task<bool> DeleteAsync(int id);
}
