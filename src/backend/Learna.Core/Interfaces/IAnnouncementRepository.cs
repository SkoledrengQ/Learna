using Learna.Core.Entities;

namespace Learna.Core.Interfaces;

public interface IAnnouncementRepository
{
    Task<Announcement?> GetByIdAsync(int id);
    Task<IReadOnlyList<Announcement>> GetAllAsync();
    Task<Announcement> AddAsync(Announcement announcement);
    Task DeleteAsync(Announcement announcement);
    Task SaveAsync();
    Task MarkReadAsync(int announcementId, int userId, DateTime readAt);
    Task<SchoolClass?> GetClassAsync(int id);
    Task<SubjectGroup?> GetSubjectGroupAsync(int id);
    Task<IReadOnlyList<SchoolClass>> GetClassesAsync();
    Task<IReadOnlyList<SubjectGroup>> GetSubjectGroupsAsync();
    Task<IReadOnlySet<int>> ActiveClassIdsForStudentAsync(int studentId);
    Task<IReadOnlySet<int>> ActiveGroupIdsForStudentAsync(int studentId);
    Task<IReadOnlySet<int>> GuardianActiveClassIdsAsync(int guardianId);
    Task<IReadOnlySet<int>> GuardianActiveGroupIdsAsync(int guardianId);
    Task<IReadOnlySet<int>> GuardianStudentIdsAsync(int guardianId);
}
