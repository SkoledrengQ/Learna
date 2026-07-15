using Learna.Core.Entities;
using Learna.Core.Interfaces;
using Learna.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Learna.Infrastructure.Repositories;

public class AttendanceRepository : IAttendanceRepository
{
    private readonly ApplicationDbContext _context;
    public AttendanceRepository(ApplicationDbContext context) => _context = context;

    public Task<List<Student>> GetRosterAsync(int subjectGroupId, DateOnly lessonDate)
    {
        var start = lessonDate.ToDateTime(TimeOnly.MinValue);
        var end = lessonDate.ToDateTime(TimeOnly.MaxValue);
        return _context.Enrollments
            .Where(e => e.SubjectGroupId == subjectGroupId && e.EnrolledDate <= end && (e.UnenrolledDate == null || e.UnenrolledDate >= start))
            .Select(e => e.Student).Distinct().OrderBy(s => s.Name.FirstName).ThenBy(s => s.Name.LastName).ToListAsync();
    }

    public Task<List<AttendanceRecord>> GetForLessonAsync(int lessonId) => _context.AttendanceRecords
        .Where(r => r.LessonId == lessonId).Include(r => r.RecordedByUser).ToListAsync();

    public async Task UpsertAsync(int lessonId, IEnumerable<(int StudentId, AttendanceStatus Status, string? Note)> records, int userId)
    {
        var now = DateTime.UtcNow;
        foreach (var item in records)
        {
            var record = await _context.AttendanceRecords.SingleOrDefaultAsync(r => r.LessonId == lessonId && r.StudentId == item.StudentId);
            if (record == null)
            {
                _context.AttendanceRecords.Add(new AttendanceRecord { LessonId = lessonId, StudentId = item.StudentId, Status = item.Status, Note = item.Note, RecordedByUserId = userId, RecordedAt = now, UpdatedAt = now });
            }
            else
            {
                record.Status = item.Status; record.Note = item.Note; record.RecordedByUserId = userId; record.UpdatedAt = now;
            }
        }
        await _context.SaveChangesAsync();
    }

    public Task<List<AttendanceRecord>> GetForStudentAsync(int studentId, DateOnly? from, DateOnly? to, DateOnly throughDate) => _context.AttendanceRecords
        .Where(r => r.StudentId == studentId && r.Lesson.Status != LessonStatus.Cancelled && r.Lesson.Date <= throughDate
            && (!from.HasValue || r.Lesson.Date >= from.Value) && (!to.HasValue || r.Lesson.Date <= to.Value))
        .Include(r => r.Lesson).ThenInclude(l => l.SubjectGroup).ThenInclude(g => g.Subject)
        .OrderByDescending(r => r.Lesson.Date).ThenByDescending(r => r.Lesson.StartTime).ToListAsync();

    public Task<bool> TeacherHasStudentAsync(int teacherId, int studentId) => _context.Enrollments
        .AnyAsync(e => e.StudentId == studentId && e.SubjectGroup.TeacherId == teacherId);
}
