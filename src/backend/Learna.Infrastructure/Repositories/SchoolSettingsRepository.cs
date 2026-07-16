using Microsoft.EntityFrameworkCore;
using Learna.Core.Entities;
using Learna.Core.Interfaces;
using Learna.Infrastructure.Data;

namespace Learna.Infrastructure.Repositories;

public class SchoolSettingsRepository : ISchoolSettingsRepository
{
    public const string DefaultSchoolName = "Learna School";
    public const string DefaultPrimaryColor = "#00695C"; // deep teal

    private readonly ApplicationDbContext _context;

    public SchoolSettingsRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<SchoolSettings> GetAsync()
    {
        var settings = await _context.SchoolSettings.FirstOrDefaultAsync();
        if (settings != null)
        {
            return settings;
        }

        settings = new SchoolSettings
        {
            SchoolName = DefaultSchoolName,
            PrimaryColor = DefaultPrimaryColor,
            CreatedAt = DateTime.UtcNow
        };
        _context.SchoolSettings.Add(settings);
        await _context.SaveChangesAsync();
        return settings;
    }

    public async Task<SchoolSettings> UpdateAsync(string schoolName, string primaryColor)
    {
        var settings = await GetAsync();
        settings.SchoolName = schoolName;
        settings.PrimaryColor = primaryColor;
        settings.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return settings;
    }
}
