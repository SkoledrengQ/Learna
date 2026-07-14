using Microsoft.EntityFrameworkCore;
using Learna.Core.Entities;
using Learna.Core.Interfaces;
using Learna.Infrastructure.Data;

namespace Learna.Infrastructure.Repositories;

public class SchoolYearRepository : ISchoolYearRepository
{
    private readonly ApplicationDbContext _context;

    public SchoolYearRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<SchoolYear>> GetAllAsync()
    {
        return await _context.SchoolYears.ToListAsync();
    }

    public async Task<SchoolYear?> GetByIdAsync(int id)
    {
        return await _context.SchoolYears.FindAsync(id);
    }

    public async Task<SchoolYear> CreateAsync(SchoolYear schoolYear)
    {
        schoolYear.CreatedAt = DateTime.UtcNow;
        _context.SchoolYears.Add(schoolYear);
        await _context.SaveChangesAsync();
        return schoolYear;
    }

    public async Task<SchoolYear> UpdateAsync(SchoolYear schoolYear)
    {
        schoolYear.UpdatedAt = DateTime.UtcNow;
        _context.SchoolYears.Update(schoolYear);
        await _context.SaveChangesAsync();
        return schoolYear;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var schoolYear = await _context.SchoolYears.FindAsync(id);
        if (schoolYear == null) return false;

        _context.SchoolYears.Remove(schoolYear);
        await _context.SaveChangesAsync();
        return true;
    }
}
