using Microsoft.EntityFrameworkCore;
using Learna.Core.Entities;
using Learna.Core.Interfaces;
using Learna.Infrastructure.Data;

namespace Learna.Infrastructure.Repositories;

public class ClassMembershipRepository : IClassMembershipRepository
{
    private readonly ApplicationDbContext _context;

    public ClassMembershipRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<ClassMembership>> GetByClassIdAsync(int classId, bool includeHistorical)
    {
        var query = _context.ClassMemberships
            .Include(m => m.Student)
            .Where(m => m.ClassId == classId);

        if (!includeHistorical)
        {
            query = query.Where(m => m.LeftDate == null);
        }

        return await query.ToListAsync();
    }

    public async Task<ClassMembership?> GetActiveByStudentAndClassAsync(int studentId, int classId)
    {
        return await _context.ClassMemberships
            .FirstOrDefaultAsync(m => m.StudentId == studentId && m.ClassId == classId && m.LeftDate == null);
    }

    public async Task<ClassMembership?> GetActiveByStudentAndSchoolYearAsync(int studentId, int schoolYearId)
    {
        return await _context.ClassMemberships
            .Include(m => m.Class)
            .FirstOrDefaultAsync(m => m.StudentId == studentId
                && m.LeftDate == null
                && m.Class.SchoolYearId == schoolYearId);
    }

    public async Task<ClassMembership> AddAsync(ClassMembership membership)
    {
        _context.ClassMemberships.Add(membership);
        await _context.SaveChangesAsync();
        return membership;
    }

    public async Task<ClassMembership> CloseAsync(ClassMembership membership, DateTime leftDate)
    {
        membership.LeftDate = leftDate;
        _context.ClassMemberships.Update(membership);
        await _context.SaveChangesAsync();
        return membership;
    }

    public async Task<ClassMembership> MoveAsync(int studentId, int fromClassId, int toClassId, DateTime moveDate)
    {
        // The in-memory provider (used in tests) doesn't support transactions.
        var transaction = _context.Database.IsRelational()
            ? await _context.Database.BeginTransactionAsync()
            : null;
        await using var _ = transaction;

        var oldMembership = await GetActiveByStudentAndClassAsync(studentId, fromClassId);
        if (oldMembership != null)
        {
            oldMembership.LeftDate = moveDate;
            _context.ClassMemberships.Update(oldMembership);
        }

        var newMembership = new ClassMembership
        {
            StudentId = studentId,
            ClassId = toClassId,
            JoinedDate = moveDate,
            LeftDate = null
        };
        _context.ClassMemberships.Add(newMembership);

        await _context.SaveChangesAsync();
        if (transaction != null)
        {
            await transaction.CommitAsync();
        }

        return newMembership;
    }
}
