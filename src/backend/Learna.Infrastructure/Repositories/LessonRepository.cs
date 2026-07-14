using Microsoft.EntityFrameworkCore;
using Learna.Core.Entities;
using Learna.Core.Interfaces;
using Learna.Infrastructure.Data;

namespace Learna.Infrastructure.Repositories;

public class LessonRepository : ILessonRepository
{
    private readonly ApplicationDbContext _context;

    public LessonRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Lesson>> GetAllAsync(DateOnly? from, DateOnly? to, int? subjectGroupId, int? roomId, int? teacherId)
    {
        var query = _context.Lessons
            .Include(l => l.SubjectGroup)
            .ThenInclude(g => g.Subject)
            .Include(l => l.Room)
            .Include(l => l.Teacher)
            .AsQueryable();

        if (from.HasValue)
        {
            query = query.Where(l => l.Date >= from.Value);
        }
        if (to.HasValue)
        {
            query = query.Where(l => l.Date <= to.Value);
        }
        if (subjectGroupId.HasValue)
        {
            query = query.Where(l => l.SubjectGroupId == subjectGroupId.Value);
        }
        if (roomId.HasValue)
        {
            query = query.Where(l => l.RoomId == roomId.Value);
        }
        if (teacherId.HasValue)
        {
            query = query.Where(l => l.TeacherId == teacherId.Value);
        }

        return await query.OrderBy(l => l.Date).ThenBy(l => l.StartTime).ToListAsync();
    }

    public async Task<Lesson?> GetByIdAsync(int id)
    {
        return await _context.Lessons
            .Include(l => l.SubjectGroup)
            .ThenInclude(g => g.Subject)
            .Include(l => l.Room)
            .Include(l => l.Teacher)
            .FirstOrDefaultAsync(l => l.Id == id);
    }

    public async Task<Lesson> AddAsync(Lesson lesson)
    {
        lesson.CreatedAt = DateTime.UtcNow;
        _context.Lessons.Add(lesson);
        await _context.SaveChangesAsync();
        return lesson;
    }

    public async Task AddRangeAsync(IEnumerable<Lesson> lessons)
    {
        var list = lessons.ToList();
        foreach (var lesson in list)
        {
            lesson.CreatedAt = DateTime.UtcNow;
        }
        _context.Lessons.AddRange(list);
        await _context.SaveChangesAsync();
    }

    public async Task<Lesson> UpdateAsync(Lesson lesson)
    {
        lesson.UpdatedAt = DateTime.UtcNow;
        _context.Lessons.Update(lesson);
        await _context.SaveChangesAsync();
        return lesson;
    }

    public async Task RemoveRangeAsync(IEnumerable<Lesson> lessons)
    {
        _context.Lessons.RemoveRange(lessons);
        await _context.SaveChangesAsync();
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var lesson = await _context.Lessons.FindAsync(id);
        if (lesson == null) return false;

        _context.Lessons.Remove(lesson);
        await _context.SaveChangesAsync();
        return true;
    }
}
