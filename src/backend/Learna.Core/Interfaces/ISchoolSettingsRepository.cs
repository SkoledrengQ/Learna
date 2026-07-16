using Learna.Core.Entities;

namespace Learna.Core.Interfaces;

public interface ISchoolSettingsRepository
{
    /// Returns the single settings row, creating it with defaults if it doesn't exist yet.
    Task<SchoolSettings> GetAsync();
    Task<SchoolSettings> UpdateAsync(string schoolName, string primaryColor);
}
