using Microsoft.EntityFrameworkCore;
using Learna.Core.Entities;
using Learna.Core.Interfaces;
using Learna.Infrastructure.Data;

namespace Learna.Infrastructure.Repositories;

public class TeacherRepository : ITeacherRepository
{
    private readonly ApplicationDbContext _context;

    public TeacherRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Teacher>> GetAllAsync()
    {
        return await _context.Teachers.ToListAsync();
    }

    public async Task<Teacher?> GetByIdAsync(int id)
    {
        return await _context.Teachers.FindAsync(id);
    }

    public async Task<Teacher> CreateAsync(Teacher teacher)
    {
        teacher.CreatedAt = DateTime.UtcNow;
        _context.Teachers.Add(teacher);
        await _context.SaveChangesAsync();
        return teacher;
    }

    public async Task<Teacher> UpdateAsync(Teacher teacher)
    {
        teacher.UpdatedAt = DateTime.UtcNow;
        _context.Teachers.Update(teacher);
        await _context.SaveChangesAsync();
        return teacher;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var teacher = await _context.Teachers.FindAsync(id);
        if (teacher == null) return false;

        _context.Teachers.Remove(teacher);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> EmailExistsAsync(string email)
    {
        return await _context.Teachers.AnyAsync(t => t.Email == email);
    }
}
