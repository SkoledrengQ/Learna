using Microsoft.EntityFrameworkCore;
using Learna.Core.Entities;
using Learna.Core.Interfaces;
using Learna.Infrastructure.Data;

namespace Learna.Infrastructure.Repositories;

public class SubjectGroupRepository : ISubjectGroupRepository
{
    private readonly ApplicationDbContext _context;

    public SubjectGroupRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<SubjectGroup>> GetAllAsync(int? termId, int? subjectId)
    {
        var query = _context.SubjectGroups.AsQueryable();
        if (termId.HasValue)
        {
            query = query.Where(g => g.TermId == termId.Value);
        }
        if (subjectId.HasValue)
        {
            query = query.Where(g => g.SubjectId == subjectId.Value);
        }
        return await query.ToListAsync();
    }

    public async Task<SubjectGroup?> GetByIdAsync(int id)
    {
        return await _context.SubjectGroups.FindAsync(id);
    }

    public async Task<SubjectGroup> CreateAsync(SubjectGroup subjectGroup)
    {
        subjectGroup.CreatedAt = DateTime.UtcNow;
        _context.SubjectGroups.Add(subjectGroup);
        await _context.SaveChangesAsync();
        return subjectGroup;
    }

    public async Task<SubjectGroup> UpdateAsync(SubjectGroup subjectGroup)
    {
        subjectGroup.UpdatedAt = DateTime.UtcNow;
        _context.SubjectGroups.Update(subjectGroup);
        await _context.SaveChangesAsync();
        return subjectGroup;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var subjectGroup = await _context.SubjectGroups.FindAsync(id);
        if (subjectGroup == null) return false;

        _context.SubjectGroups.Remove(subjectGroup);
        await _context.SaveChangesAsync();
        return true;
    }
}
