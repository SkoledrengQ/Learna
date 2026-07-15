using Learna.Core.Entities;
using Learna.Core.Interfaces;
using Learna.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Learna.Infrastructure.Repositories;

public sealed class AnnouncementRepository(ApplicationDbContext context) : IAnnouncementRepository
{
    private IQueryable<Announcement> Announcements => context.Announcements
        .Include(a => a.CreatedByUser)
        .Include(a => a.Reads)
        .Include(a => a.Files).ThenInclude(f => f.UploadedByUser);

    public Task<Announcement?> GetByIdAsync(int id) => Announcements.FirstOrDefaultAsync(a => a.Id == id);
    public async Task<IReadOnlyList<Announcement>> GetAllAsync() => await Announcements.OrderByDescending(a => a.PublishAt).ThenByDescending(a => a.CreatedAt).ToListAsync();
    public async Task<Announcement> AddAsync(Announcement announcement) { context.Announcements.Add(announcement); await context.SaveChangesAsync(); return announcement; }
    public async Task DeleteAsync(Announcement announcement) { context.Announcements.Remove(announcement); await context.SaveChangesAsync(); }
    public Task SaveAsync() => context.SaveChangesAsync();

    public async Task MarkReadAsync(int announcementId, int userId, DateTime readAt)
    {
        var existing = await context.AnnouncementReads.FirstOrDefaultAsync(r => r.AnnouncementId == announcementId && r.UserId == userId);
        if (existing != null) return;
        context.AnnouncementReads.Add(new AnnouncementRead { AnnouncementId = announcementId, UserId = userId, ReadAt = readAt });
        await context.SaveChangesAsync();
    }

    public Task<SchoolClass?> GetClassAsync(int id) => context.SchoolClasses.FirstOrDefaultAsync(c => c.Id == id);
    public Task<SubjectGroup?> GetSubjectGroupAsync(int id) => context.SubjectGroups.FirstOrDefaultAsync(g => g.Id == id);
    public async Task<IReadOnlyList<SchoolClass>> GetClassesAsync() => await context.SchoolClasses.OrderBy(c => c.Name).ToListAsync();
    public async Task<IReadOnlyList<SubjectGroup>> GetSubjectGroupsAsync() => await context.SubjectGroups.OrderBy(g => g.Name).ToListAsync();
    public async Task<IReadOnlySet<int>> ActiveClassIdsForStudentAsync(int studentId) => (await context.ClassMemberships.Where(m => m.StudentId == studentId && m.LeftDate == null).Select(m => m.ClassId).ToListAsync()).ToHashSet();
    public async Task<IReadOnlySet<int>> ActiveGroupIdsForStudentAsync(int studentId) => (await context.Enrollments.Where(e => e.StudentId == studentId && e.UnenrolledDate == null).Select(e => e.SubjectGroupId).ToListAsync()).ToHashSet();
    public async Task<IReadOnlySet<int>> GuardianStudentIdsAsync(int guardianId) => (await context.StudentGuardians.Where(sg => sg.GuardianId == guardianId).Select(sg => sg.StudentId).ToListAsync()).ToHashSet();
    public async Task<IReadOnlySet<int>> GuardianActiveClassIdsAsync(int guardianId) => (await context.StudentGuardians.Where(sg => sg.GuardianId == guardianId).SelectMany(sg => context.ClassMemberships.Where(m => m.StudentId == sg.StudentId && m.LeftDate == null).Select(m => m.ClassId)).ToListAsync()).ToHashSet();
    public async Task<IReadOnlySet<int>> GuardianActiveGroupIdsAsync(int guardianId) => (await context.StudentGuardians.Where(sg => sg.GuardianId == guardianId).SelectMany(sg => context.Enrollments.Where(e => e.StudentId == sg.StudentId && e.UnenrolledDate == null).Select(e => e.SubjectGroupId)).ToListAsync()).ToHashSet();
}
