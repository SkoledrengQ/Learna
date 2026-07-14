using Microsoft.EntityFrameworkCore;
using Learna.Core.Entities;
using Learna.Core.Interfaces;
using Learna.Infrastructure.Data;

namespace Learna.Infrastructure.Repositories;

public class GuardianRepository : IGuardianRepository
{
    private readonly ApplicationDbContext _context;

    public GuardianRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Guardian?> GetByIdAsync(int id)
    {
        return await _context.Guardians.FindAsync(id);
    }

    public async Task<Guardian> CreateAsync(Guardian guardian)
    {
        guardian.CreatedAt = DateTime.UtcNow;
        _context.Guardians.Add(guardian);
        await _context.SaveChangesAsync();
        return guardian;
    }

    public async Task<Guardian> UpdateAsync(Guardian guardian)
    {
        guardian.UpdatedAt = DateTime.UtcNow;
        _context.Guardians.Update(guardian);
        await _context.SaveChangesAsync();
        return guardian;
    }

    public async Task<StudentGuardian?> GetLinkAsync(int studentId, int guardianId)
    {
        return await _context.StudentGuardians
            .Include(sg => sg.Guardian)
            .FirstOrDefaultAsync(sg => sg.StudentId == studentId && sg.GuardianId == guardianId);
    }

    public async Task<StudentGuardian> LinkAsync(StudentGuardian link)
    {
        _context.StudentGuardians.Add(link);
        await _context.SaveChangesAsync();
        return link;
    }

    public async Task<StudentGuardian> UpdateLinkAsync(StudentGuardian link)
    {
        _context.StudentGuardians.Update(link);
        await _context.SaveChangesAsync();
        return link;
    }

    public async Task<bool> UnlinkAsync(int studentId, int guardianId)
    {
        var link = await _context.StudentGuardians
            .FirstOrDefaultAsync(sg => sg.StudentId == studentId && sg.GuardianId == guardianId);
        if (link == null) return false;

        _context.StudentGuardians.Remove(link);
        await _context.SaveChangesAsync();
        return true;
    }
}
