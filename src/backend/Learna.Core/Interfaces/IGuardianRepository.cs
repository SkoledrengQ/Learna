using Learna.Core.Entities;

namespace Learna.Core.Interfaces;

public interface IGuardianRepository
{
    Task<Guardian?> GetByIdAsync(int id);
    Task<Guardian> CreateAsync(Guardian guardian);
    Task<Guardian> UpdateAsync(Guardian guardian);
    Task<StudentGuardian?> GetLinkAsync(int studentId, int guardianId);
    Task<StudentGuardian> LinkAsync(StudentGuardian link);
    Task<StudentGuardian> UpdateLinkAsync(StudentGuardian link);
    Task<bool> UnlinkAsync(int studentId, int guardianId);
}
