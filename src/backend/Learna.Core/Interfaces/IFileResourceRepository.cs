using Learna.Core.Entities;

namespace Learna.Core.Interfaces;

public record VisibleFileResource(FileResource File, int? ChildId, string? ChildName);

public interface IFileResourceRepository
{
    Task<FileResource?> GetByIdAsync(int id);
    Task<IReadOnlyList<FileResource>> GetForSubjectGroupAsync(int subjectGroupId);
    Task<IReadOnlyList<FileResource>> GetForLessonAsync(int lessonId);
    Task<IReadOnlyList<FileResource>> GetForAssignmentAsync(int assignmentId);
    Task<IReadOnlyList<FileResource>> GetForAnnouncementAsync(int announcementId);
    Task<IReadOnlyList<VisibleFileResource>> GetVisibleForUserAsync(User user, bool isAdmin);
    Task<SubjectGroup?> GetSubjectGroupAsync(int id);
    Task<Lesson?> GetLessonAsync(int id);
    Task<Assignment?> GetAssignmentAsync(int id);
    Task<Announcement?> GetAnnouncementAsync(int id);
    Task<bool> HasActiveEnrollmentAsync(int studentId, int subjectGroupId);
    Task<bool> GuardianHasActiveEnrollmentAsync(int guardianId, int subjectGroupId);
    Task<FileResource> AddAsync(FileResource file);
    Task DeleteAsync(FileResource file);
}
