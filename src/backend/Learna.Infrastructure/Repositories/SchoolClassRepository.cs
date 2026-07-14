using Microsoft.EntityFrameworkCore;
using Learna.Core.Entities;
using Learna.Core.Interfaces;
using Learna.Infrastructure.Data;

namespace Learna.Infrastructure.Repositories;

public class SchoolClassRepository : ISchoolClassRepository
{
    private readonly ApplicationDbContext _context;

    public SchoolClassRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<SchoolClass>> GetAllAsync(int? schoolYearId)
    {
        var query = _context.SchoolClasses.AsQueryable();
        if (schoolYearId.HasValue)
        {
            query = query.Where(c => c.SchoolYearId == schoolYearId.Value);
        }
        return await query.ToListAsync();
    }

    public async Task<SchoolClass?> GetByIdAsync(int id)
    {
        return await _context.SchoolClasses.FindAsync(id);
    }

    public async Task<SchoolClass> CreateAsync(SchoolClass schoolClass)
    {
        schoolClass.CreatedAt = DateTime.UtcNow;
        _context.SchoolClasses.Add(schoolClass);
        await _context.SaveChangesAsync();
        return schoolClass;
    }

    public async Task<SchoolClass> UpdateAsync(SchoolClass schoolClass)
    {
        schoolClass.UpdatedAt = DateTime.UtcNow;
        _context.SchoolClasses.Update(schoolClass);
        await _context.SaveChangesAsync();
        return schoolClass;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var schoolClass = await _context.SchoolClasses.FindAsync(id);
        if (schoolClass == null) return false;

        _context.SchoolClasses.Remove(schoolClass);
        await _context.SaveChangesAsync();
        return true;
    }
}
