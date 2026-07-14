using Learna.Core.Entities;

namespace Learna.Core.Interfaces;

public interface IEnrollmentRepository
{
    Task<IEnumerable<Enrollment>> GetBySubjectGroupIdAsync(int subjectGroupId, bool includeHistorical);
    Task<Enrollment?> GetActiveBySubjectGroupAndStudentAsync(int subjectGroupId, int studentId);
    Task<Enrollment> AddAsync(Enrollment enrollment);
    Task<Enrollment> CloseAsync(Enrollment enrollment, DateTime unenrolledDate);
}
