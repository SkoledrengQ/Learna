using Learna.Core.Entities;

namespace Learna.Core.Interfaces;

public interface ISubjectGroupRepository
{
    Task<IEnumerable<SubjectGroup>> GetAllAsync(int? termId, int? subjectId);
    Task<SubjectGroup?> GetByIdAsync(int id);
    Task<SubjectGroup> CreateAsync(SubjectGroup subjectGroup);
    Task<SubjectGroup> UpdateAsync(SubjectGroup subjectGroup);
    Task<bool> DeleteAsync(int id);
}
