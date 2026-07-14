using Learna.Core.Entities;

namespace Learna.Core.Interfaces;

public interface ISchoolClassRepository
{
    Task<IEnumerable<SchoolClass>> GetAllAsync(int? schoolYearId);
    Task<SchoolClass?> GetByIdAsync(int id);
    Task<SchoolClass> CreateAsync(SchoolClass schoolClass);
    Task<SchoolClass> UpdateAsync(SchoolClass schoolClass);
    Task<bool> DeleteAsync(int id);
}
