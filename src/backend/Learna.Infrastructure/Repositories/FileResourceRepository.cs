using Learna.Core.Entities;
using Learna.Core.Interfaces;
using Learna.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Learna.Infrastructure.Repositories;

public sealed class FileResourceRepository(ApplicationDbContext context) : IFileResourceRepository
{
    private IQueryable<FileResource> Files => context.FileResources
        .Include(f => f.UploadedByUser)
        .Include(f => f.SubjectGroup)!.ThenInclude(g => g!.Subject)
        .Include(f => f.Lesson)!.ThenInclude(l => l!.SubjectGroup).ThenInclude(g => g.Subject)
        .Include(f => f.Assignment)!.ThenInclude(a => a!.SubjectGroup)
        .Include(f => f.Submission)!.ThenInclude(s => s!.Assignment).ThenInclude(a => a.SubjectGroup)
        .Include(f => f.Announcement);

    public Task<FileResource?> GetByIdAsync(int id) => Files.FirstOrDefaultAsync(f => f.Id == id);
    public async Task<IReadOnlyList<FileResource>> GetForSubjectGroupAsync(int subjectGroupId) => await Files.Where(f => f.SubjectGroupId == subjectGroupId || (f.LessonId != null && f.Lesson!.SubjectGroupId == subjectGroupId)).OrderByDescending(f => f.CreatedAt).ToListAsync();
    public async Task<IReadOnlyList<FileResource>> GetForLessonAsync(int lessonId) => await Files.Where(f => f.LessonId == lessonId).OrderByDescending(f => f.CreatedAt).ToListAsync();
    public async Task<IReadOnlyList<FileResource>> GetForAssignmentAsync(int assignmentId) => await Files.Where(f => f.AssignmentId == assignmentId).OrderByDescending(f => f.CreatedAt).ToListAsync();
    public async Task<IReadOnlyList<FileResource>> GetForAnnouncementAsync(int announcementId) => await Files.Where(f => f.AnnouncementId == announcementId).OrderByDescending(f => f.CreatedAt).ToListAsync();
    public Task<SubjectGroup?> GetSubjectGroupAsync(int id) => context.SubjectGroups.Include(g => g.Subject).FirstOrDefaultAsync(g => g.Id == id);
    public Task<Lesson?> GetLessonAsync(int id) => context.Lessons.Include(l => l.SubjectGroup).ThenInclude(g => g.Subject).FirstOrDefaultAsync(l => l.Id == id);
    public Task<Assignment?> GetAssignmentAsync(int id) => context.Assignments.Include(a => a.SubjectGroup).Include(a => a.Extensions).FirstOrDefaultAsync(a => a.Id == id);
    public Task<Announcement?> GetAnnouncementAsync(int id) => context.Announcements.Include(a => a.Reads).Include(a => a.Files).FirstOrDefaultAsync(a => a.Id == id);
    public Task<bool> HasActiveEnrollmentAsync(int studentId, int subjectGroupId) => context.Enrollments.AnyAsync(e => e.StudentId == studentId && e.SubjectGroupId == subjectGroupId && e.UnenrolledDate == null);
    public Task<bool> GuardianHasActiveEnrollmentAsync(int guardianId, int subjectGroupId) => context.Enrollments.AnyAsync(e => e.SubjectGroupId == subjectGroupId && e.UnenrolledDate == null && context.StudentGuardians.Any(sg => sg.GuardianId == guardianId && sg.StudentId == e.StudentId));

    public async Task<IReadOnlyList<VisibleFileResource>> GetVisibleForUserAsync(User user, bool isAdmin)
    {
        var materials = Files.Where(f => f.SubjectGroupId != null || f.LessonId != null);
        if (isAdmin) return (await materials.OrderByDescending(f => f.CreatedAt).ToListAsync()).Select(f => new VisibleFileResource(f, null, null)).ToList();
        if (user.StudentId is int studentId)
        {
            var groupIds = context.Enrollments.Where(e => e.StudentId == studentId && e.UnenrolledDate == null).Select(e => e.SubjectGroupId);
            return (await materials.Where(f => groupIds.Contains(f.SubjectGroupId ?? f.Lesson!.SubjectGroupId)).OrderByDescending(f => f.CreatedAt).ToListAsync()).Select(f => new VisibleFileResource(f, null, null)).ToList();
        }
        if (user.GuardianId is int guardianId)
        {
            var rows = await context.StudentGuardians.Where(sg => sg.GuardianId == guardianId)
                .SelectMany(sg => context.Enrollments.Where(e => e.StudentId == sg.StudentId && e.UnenrolledDate == null), (sg, e) => new { sg.Student, e.SubjectGroupId })
                .ToListAsync();
            var result = new List<VisibleFileResource>();
            foreach (var row in rows)
            {
                var files = await materials.Where(f => f.SubjectGroupId == row.SubjectGroupId || (f.LessonId != null && f.Lesson!.SubjectGroupId == row.SubjectGroupId)).ToListAsync();
                result.AddRange(files.Select(f => new VisibleFileResource(f, row.Student.Id, $"{row.Student.Name.FirstName} {row.Student.Name.LastName}")));
            }
            return result.OrderByDescending(x => x.File.CreatedAt).ToList();
        }
        if (user.TeacherId is int teacherId)
            return (await materials.Where(f => (f.SubjectGroupId != null && f.SubjectGroup!.TeacherId == teacherId) || (f.LessonId != null && f.Lesson!.TeacherId == teacherId)).OrderByDescending(f => f.CreatedAt).ToListAsync()).Select(f => new VisibleFileResource(f, null, null)).ToList();
        return Array.Empty<VisibleFileResource>();
    }

    public async Task<FileResource> AddAsync(FileResource file) { context.FileResources.Add(file); await context.SaveChangesAsync(); return file; }
    public async Task DeleteAsync(FileResource file) { context.FileResources.Remove(file); await context.SaveChangesAsync(); }
}
