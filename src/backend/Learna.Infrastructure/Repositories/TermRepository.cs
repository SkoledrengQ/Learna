using Microsoft.EntityFrameworkCore;
using Learna.Core.Entities;
using Learna.Core.Interfaces;
using Learna.Infrastructure.Data;

namespace Learna.Infrastructure.Repositories;

public class TermRepository : ITermRepository
{
    private readonly ApplicationDbContext _context;

    public TermRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Term>> GetBySchoolYearIdAsync(int schoolYearId)
    {
        return await _context.Terms
            .Where(t => t.SchoolYearId == schoolYearId)
            .ToListAsync();
    }

    public async Task<Term?> GetByIdAsync(int id)
    {
        return await _context.Terms.FindAsync(id);
    }

    public async Task<Term> CreateAsync(Term term)
    {
        term.CreatedAt = DateTime.UtcNow;
        _context.Terms.Add(term);
        await _context.SaveChangesAsync();
        return term;
    }

    public async Task<Term> UpdateAsync(Term term)
    {
        term.UpdatedAt = DateTime.UtcNow;
        _context.Terms.Update(term);
        await _context.SaveChangesAsync();
        return term;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var term = await _context.Terms.FindAsync(id);
        if (term == null) return false;

        _context.Terms.Remove(term);
        await _context.SaveChangesAsync();
        return true;
    }
}
