using Microsoft.EntityFrameworkCore;
using Learna.Core.Entities;
using Learna.Core.Interfaces;
using Learna.Infrastructure.Data;

namespace Learna.Infrastructure.Repositories;

public class EnrollmentRepository : IEnrollmentRepository
{
    private readonly ApplicationDbContext _context;

    public EnrollmentRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Enrollment>> GetBySubjectGroupIdAsync(int subjectGroupId, bool includeHistorical)
    {
        var query = _context.Enrollments
            .Include(e => e.Student)
            .Where(e => e.SubjectGroupId == subjectGroupId);

        if (!includeHistorical)
        {
            query = query.Where(e => e.UnenrolledDate == null);
        }

        return await query.ToListAsync();
    }

    public async Task<IEnumerable<Enrollment>> GetActiveByStudentIdAsync(int studentId)
    {
        return await _context.Enrollments
            .Where(e => e.StudentId == studentId && e.UnenrolledDate == null)
            .ToListAsync();
    }

    public async Task<Enrollment?> GetActiveBySubjectGroupAndStudentAsync(int subjectGroupId, int studentId)
    {
        return await _context.Enrollments
            .FirstOrDefaultAsync(e => e.SubjectGroupId == subjectGroupId
                && e.StudentId == studentId
                && e.UnenrolledDate == null);
    }

    public async Task<Enrollment> AddAsync(Enrollment enrollment)
    {
        _context.Enrollments.Add(enrollment);
        await _context.SaveChangesAsync();
        return enrollment;
    }

    public async Task<Enrollment> CloseAsync(Enrollment enrollment, DateTime unenrolledDate)
    {
        enrollment.UnenrolledDate = unenrolledDate;
        _context.Enrollments.Update(enrollment);
        await _context.SaveChangesAsync();
        return enrollment;
    }
}
