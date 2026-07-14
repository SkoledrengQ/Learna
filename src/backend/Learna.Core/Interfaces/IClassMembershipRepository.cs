using Learna.Core.Entities;

namespace Learna.Core.Interfaces;

public interface IClassMembershipRepository
{
    Task<IEnumerable<ClassMembership>> GetByClassIdAsync(int classId, bool includeHistorical);
    Task<ClassMembership?> GetActiveByStudentAndClassAsync(int studentId, int classId);
    Task<ClassMembership?> GetActiveByStudentAndSchoolYearAsync(int studentId, int schoolYearId);
    Task<ClassMembership> AddAsync(ClassMembership membership);
    Task<ClassMembership> CloseAsync(ClassMembership membership, DateTime leftDate);

    // Closes the student's active membership in fromClassId and opens a new one in
    // toClassId, in a single transaction, preserving history.
    Task<ClassMembership> MoveAsync(int studentId, int fromClassId, int toClassId, DateTime moveDate);
}
