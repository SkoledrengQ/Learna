using Learna.Core.Entities;
using Learna.Core.Interfaces;
using Learna.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Learna.Infrastructure.Repositories;

public sealed class GradeRepository(ApplicationDbContext context) : IGradeRepository
{
    private IQueryable<Grade> Query => context.Grades
        .Include(g => g.Student)
        .Include(g => g.SubjectGroup).ThenInclude(g => g.Subject)
        .Include(g => g.Submission).ThenInclude(s => s!.Assignment).ThenInclude(a => a.Submissions);

    public Task<Grade?> GetAsync(int id) => Query.FirstOrDefaultAsync(g => g.Id == id);
    public Task<Grade?> GetForSubmissionAsync(int submissionId) => Query.FirstOrDefaultAsync(g => g.SubmissionId == submissionId);
    public Task<Submission?> GetSubmissionAsync(int submissionId) => context.Submissions.Include(s => s.Assignment).ThenInclude(a => a.SubjectGroup).FirstOrDefaultAsync(s => s.Id == submissionId);
    public Task<Assignment?> GetAssignmentAsync(int assignmentId) => context.Assignments.Include(a => a.SubjectGroup).Include(a => a.Submissions).FirstOrDefaultAsync(a => a.Id == assignmentId);
    public Task<SubjectGroup?> GetGroupAsync(int groupId) => context.SubjectGroups.Include(g => g.Subject).FirstOrDefaultAsync(g => g.Id == groupId);
    public Task<bool> IsEnrolledAsync(int groupId, int studentId) => context.Enrollments.AnyAsync(e => e.SubjectGroupId == groupId && e.StudentId == studentId && e.UnenrolledDate == null);
    public Task<bool> GuardianOwnsStudentAsync(int guardianId, int studentId) => context.StudentGuardians.AnyAsync(x => x.GuardianId == guardianId && x.StudentId == studentId);
    public async Task<IReadOnlyList<Grade>> GetForGroupAsync(int groupId) => (await Query.Where(g => g.SubjectGroupId == groupId).ToListAsync()).OrderBy(g => g.Category).ThenBy(g => g.Student.Name.FirstName).ToList();
    public async Task<IReadOnlyList<Grade>> GetPublishedForStudentAsync(int studentId) => await Query.Where(g => g.StudentId == studentId && g.Status == GradeStatus.Published).OrderByDescending(g => g.PublishedAt).ToListAsync();
    public async Task<IReadOnlyList<Grade>> GetForAssignmentAsync(int assignmentId) => await Query.Where(g => g.Submission != null && g.Submission.AssignmentId == assignmentId).ToListAsync();
    public async Task AddAsync(Grade grade) { context.Grades.Add(grade); await context.SaveChangesAsync(); }
    public async Task DeleteAsync(Grade grade) { context.Grades.Remove(grade); await context.SaveChangesAsync(); }
    public Task SaveAsync() => context.SaveChangesAsync();
}
