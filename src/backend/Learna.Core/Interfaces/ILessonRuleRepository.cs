using Learna.Core.Entities;

namespace Learna.Core.Interfaces;

public interface ILessonRuleRepository
{
    Task<IEnumerable<LessonRule>> GetBySubjectGroupIdAsync(int subjectGroupId);
    Task<LessonRule?> GetByIdAsync(int id);
    Task<LessonRule?> GetByIdWithLessonsAsync(int id);
    Task<LessonRule> AddAsync(LessonRule rule);
    Task<LessonRule> UpdateAsync(LessonRule rule);
    Task DeleteAsync(LessonRule rule);
}
