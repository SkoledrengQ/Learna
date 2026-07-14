using Learna.Core.Entities;

namespace Learna.Core.Interfaces;

public interface ILessonRepository
{
    Task<IEnumerable<Lesson>> GetAllAsync(DateOnly? from, DateOnly? to, int? subjectGroupId, int? roomId, int? teacherId);
    Task<Lesson?> GetByIdAsync(int id);
    Task<Lesson> AddAsync(Lesson lesson);
    Task AddRangeAsync(IEnumerable<Lesson> lessons);
    Task<Lesson> UpdateAsync(Lesson lesson);
    Task RemoveRangeAsync(IEnumerable<Lesson> lessons);
    Task<bool> DeleteAsync(int id);
}
