using Learna.Core.Entities;
using Learna.Core.Interfaces;
using Learna.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Learna.Infrastructure.Repositories;

public sealed class AssignmentRepository(ApplicationDbContext context) : IAssignmentRepository
{
    private IQueryable<Assignment> Query => context.Assignments
        .Include(a => a.SubjectGroup).ThenInclude(g => g.Subject)
        .Include(a => a.Extensions)
        .Include(a => a.Submissions).ThenInclude(s => s.Files)
        .Include(a => a.Submissions).ThenInclude(s => s.Grade)
        .Include(a => a.Files).ThenInclude(f => f.UploadedByUser);

    public Task<Assignment?> GetAsync(int id) => Query.FirstOrDefaultAsync(a => a.Id == id);
    public async Task<IReadOnlyList<Assignment>> GetForGroupAsync(int groupId) => await Query.Where(a => a.SubjectGroupId == groupId).OrderByDescending(a => a.DeadlineDate).ThenByDescending(a => a.DeadlineTime).ToListAsync();
    public async Task<IReadOnlyList<Assignment>> GetForStudentAsync(int studentId) => await Query.Where(a => a.Status != AssignmentStatus.Draft && a.Status != AssignmentStatus.Archived && context.Enrollments.Any(e => e.SubjectGroupId == a.SubjectGroupId && e.StudentId == studentId && e.UnenrolledDate == null)).OrderBy(a => a.DeadlineDate).ThenBy(a => a.DeadlineTime).ToListAsync();
    public async Task<IReadOnlyList<Assignment>> GetForGuardianChildAsync(int guardianId, int studentId)
    {
        if (!await context.StudentGuardians.AnyAsync(sg => sg.GuardianId == guardianId && sg.StudentId == studentId)) return Array.Empty<Assignment>();
        return await GetForStudentAsync(studentId);
    }
    public async Task<IReadOnlyList<SubjectGroup>> GetTeacherGroupsAsync(int teacherId) => await context.SubjectGroups.Include(g => g.Subject).Include(g => g.Term).Where(g => g.TeacherId == teacherId).OrderBy(g => g.Name).ToListAsync();
    public Task<bool> IsEnrolledAsync(int groupId, int studentId) => context.Enrollments.AnyAsync(e => e.SubjectGroupId == groupId && e.StudentId == studentId && e.UnenrolledDate == null);
    public Task<bool> GuardianOwnsStudentAsync(int guardianId, int studentId) => context.StudentGuardians.AnyAsync(sg => sg.GuardianId == guardianId && sg.StudentId == studentId);
    public async Task<Assignment> AddAsync(Assignment assignment) { context.Add(assignment); await context.SaveChangesAsync(); return assignment; }
    public Task SaveAsync() => context.SaveChangesAsync();
    public async Task DeleteAsync(Assignment assignment) { context.Remove(assignment); await context.SaveChangesAsync(); }
    public Task<AssignmentExtension?> GetExtensionAsync(int assignmentId, int studentId) => context.AssignmentExtensions.FirstOrDefaultAsync(e => e.AssignmentId == assignmentId && e.StudentId == studentId);
    public async Task UpsertExtensionAsync(AssignmentExtension extension) { if (extension.Id == 0) context.Add(extension); await context.SaveChangesAsync(); }
    public async Task DeleteExtensionAsync(AssignmentExtension extension) { context.Remove(extension); await context.SaveChangesAsync(); }
    public Task<Submission?> GetSubmissionAsync(int assignmentId, int studentId) => context.Submissions.Include(s => s.Files).Include(s => s.Grade).FirstOrDefaultAsync(s => s.AssignmentId == assignmentId && s.StudentId == studentId);
    public async Task<IReadOnlyList<Enrollment>> GetRosterAsync(int groupId) => await context.Enrollments.Include(e => e.Student).Where(e => e.SubjectGroupId == groupId && e.UnenrolledDate == null).OrderBy(e => e.Student.Name.FirstName).ThenBy(e => e.Student.Name.LastName).ToListAsync();
    public async Task<IReadOnlyList<Submission>> GetSubmissionsAsync(int assignmentId) => await context.Submissions.Include(s => s.Student).Include(s => s.Files).Include(s => s.Grade).Where(s => s.AssignmentId == assignmentId).ToListAsync();
    public async Task<FileResource> AddFileAsync(FileResource file) { context.Add(file); await context.SaveChangesAsync(); return file; }
    public async Task<IReadOnlyList<FileResource>> GetAssignmentFilesAsync(int assignmentId) => await context.FileResources.Include(f => f.UploadedByUser).Where(f => f.AssignmentId == assignmentId).OrderByDescending(f => f.CreatedAt).ToListAsync();
    public async Task<IReadOnlyList<FileResource>> GetSubmissionFilesAsync(int submissionId) => await context.FileResources.Include(f => f.UploadedByUser).Where(f => f.SubmissionId == submissionId).OrderByDescending(f => f.CreatedAt).ToListAsync();
    public async Task RemoveSubmissionFilesAsync(int submissionId) { context.FileResources.RemoveRange(context.FileResources.Where(f => f.SubmissionId == submissionId)); await context.SaveChangesAsync(); }
}
