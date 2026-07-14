using Microsoft.EntityFrameworkCore;
using Learna.Core.Entities;
using Learna.Core.Interfaces;
using Learna.Infrastructure.Data;

namespace Learna.Infrastructure.Repositories;

public class SubjectRepository : ISubjectRepository
{
    private readonly ApplicationDbContext _context;

    public SubjectRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Subject>> GetAllAsync()
    {
        return await _context.Subjects.ToListAsync();
    }

    public async Task<Subject?> GetByIdAsync(int id)
    {
        return await _context.Subjects.FindAsync(id);
    }

    public async Task<Subject> CreateAsync(Subject subject)
    {
        subject.CreatedAt = DateTime.UtcNow;
        _context.Subjects.Add(subject);
        await _context.SaveChangesAsync();
        return subject;
    }

    public async Task<Subject> UpdateAsync(Subject subject)
    {
        subject.UpdatedAt = DateTime.UtcNow;
        _context.Subjects.Update(subject);
        await _context.SaveChangesAsync();
        return subject;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var subject = await _context.Subjects.FindAsync(id);
        if (subject == null) return false;

        _context.Subjects.Remove(subject);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> CodeExistsAsync(string code)
    {
        return await _context.Subjects.AnyAsync(s => s.Code == code);
    }
}
