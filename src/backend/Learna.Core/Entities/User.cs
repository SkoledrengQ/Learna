namespace Learna.Core.Entities;

public class User
{
    public int Id { get; set; }
    public required string Email { get; set; }
    public required string PasswordHash { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public bool IsActive { get; set; } = true;

    /// Nullable; "en"/"th" for now. Null means no preference set yet (frontend falls back to localStorage/default).
    public string? PreferredLanguage { get; set; }

    // Navigation properties
    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();

    // Foreign keys to domain entities (nullable - not all users are students/teachers)
    public int? StudentId { get; set; }
    public Student? Student { get; set; }

    public int? GuardianId { get; set; }
    public Guardian? Guardian { get; set; }

    public int? TeacherId { get; set; }
    public Teacher? Teacher { get; set; }
}
