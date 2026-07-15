using Learna.Core.Entities;

namespace Learna.Core.Interfaces;

public interface IAssignmentRepository
{
    Task<Assignment?> GetAsync(int id);
    Task<IReadOnlyList<Assignment>> GetForGroupAsync(int groupId);
    Task<IReadOnlyList<Assignment>> GetForStudentAsync(int studentId);
    Task<IReadOnlyList<Assignment>> GetForGuardianChildAsync(int guardianId, int studentId);
    Task<IReadOnlyList<SubjectGroup>> GetTeacherGroupsAsync(int teacherId);
    Task<bool> IsEnrolledAsync(int groupId, int studentId);
    Task<bool> GuardianOwnsStudentAsync(int guardianId, int studentId);
    Task<Assignment> AddAsync(Assignment assignment);
    Task SaveAsync();
    Task DeleteAsync(Assignment assignment);
    Task<AssignmentExtension?> GetExtensionAsync(int assignmentId, int studentId);
    Task UpsertExtensionAsync(AssignmentExtension extension);
    Task DeleteExtensionAsync(AssignmentExtension extension);
    Task<Submission?> GetSubmissionAsync(int assignmentId, int studentId);
    Task<IReadOnlyList<Enrollment>> GetRosterAsync(int groupId);
    Task<IReadOnlyList<Submission>> GetSubmissionsAsync(int assignmentId);
    Task<FileResource> AddFileAsync(FileResource file);
    Task<IReadOnlyList<FileResource>> GetAssignmentFilesAsync(int assignmentId);
    Task<IReadOnlyList<FileResource>> GetSubmissionFilesAsync(int submissionId);
    Task RemoveSubmissionFilesAsync(int submissionId);
}
