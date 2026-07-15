using Learna.Core.Entities;

namespace Learna.Core.Interfaces;

public interface IGradeRepository
{
    Task<Grade?> GetAsync(int id);
    Task<Grade?> GetForSubmissionAsync(int submissionId);
    Task<Submission?> GetSubmissionAsync(int submissionId);
    Task<Assignment?> GetAssignmentAsync(int assignmentId);
    Task<SubjectGroup?> GetGroupAsync(int groupId);
    Task<bool> IsEnrolledAsync(int groupId, int studentId);
    Task<bool> GuardianOwnsStudentAsync(int guardianId, int studentId);
    Task<IReadOnlyList<Grade>> GetForGroupAsync(int groupId);
    Task<IReadOnlyList<Grade>> GetPublishedForStudentAsync(int studentId);
    Task<IReadOnlyList<Grade>> GetForAssignmentAsync(int assignmentId);
    Task AddAsync(Grade grade);
    Task DeleteAsync(Grade grade);
    Task SaveAsync();
}
