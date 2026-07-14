using Microsoft.EntityFrameworkCore;
using Learna.Core.Entities;
using Learna.Core.Interfaces;
using Learna.Infrastructure.Data;

namespace Learna.Infrastructure.Repositories;

public class LessonRuleRepository : ILessonRuleRepository
{
    private readonly ApplicationDbContext _context;

    public LessonRuleRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<LessonRule>> GetBySubjectGroupIdAsync(int subjectGroupId)
    {
        return await _context.LessonRules
            .Include(r => r.Room)
            .Where(r => r.SubjectGroupId == subjectGroupId)
            .OrderBy(r => r.DayOfWeek).ThenBy(r => r.StartTime)
            .ToListAsync();
    }

    public async Task<LessonRule?> GetByIdAsync(int id)
    {
        return await _context.LessonRules
            .Include(r => r.Room)
            .FirstOrDefaultAsync(r => r.Id == id);
    }

    public async Task<LessonRule?> GetByIdWithLessonsAsync(int id)
    {
        return await _context.LessonRules
            .Include(r => r.Room)
            .Include(r => r.Lessons)
            .FirstOrDefaultAsync(r => r.Id == id);
    }

    public async Task<LessonRule> AddAsync(LessonRule rule)
    {
        rule.CreatedAt = DateTime.UtcNow;
        _context.LessonRules.Add(rule);
        await _context.SaveChangesAsync();
        return rule;
    }

    public async Task<LessonRule> UpdateAsync(LessonRule rule)
    {
        rule.UpdatedAt = DateTime.UtcNow;
        _context.LessonRules.Update(rule);
        await _context.SaveChangesAsync();
        return rule;
    }

    public async Task DeleteAsync(LessonRule rule)
    {
        _context.LessonRules.Remove(rule);
        await _context.SaveChangesAsync();
    }
}
