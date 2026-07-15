using Learna.Core.Entities;

namespace Learna.Core.Interfaces;

public interface IAttendanceRepository
{
    Task<List<Student>> GetRosterAsync(int subjectGroupId, DateOnly lessonDate);
    Task<List<AttendanceRecord>> GetForLessonAsync(int lessonId);
    Task UpsertAsync(int lessonId, IEnumerable<(int StudentId, AttendanceStatus Status, string? Note)> records, int userId);
    Task<List<AttendanceRecord>> GetForStudentAsync(int studentId, DateOnly? from, DateOnly? to, DateOnly throughDate);
    Task<bool> TeacherHasStudentAsync(int teacherId, int studentId);
}
